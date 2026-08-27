using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Gives the migrated users their passwords back.
/// <para>
/// <b>Why it exists.</b> The tenancy step created 3,878 users and never touched their passwords,
/// so every one of them had a null <c>PasswordHash</c> and not a single legacy user could sign in.
/// The eight accounts that did have one were the seeded administrator and the test users.
/// </para>
/// <para>
/// <b>Decrypt, then hash.</b> The legacy column is reversibly encrypted, so the plaintext can be
/// recovered — and must not stay recoverable. Each value is decrypted with <see cref="LegacyCrypt"/>
/// and immediately re-protected as a one-way ASP.NET Core Identity hash. The hashing is done by
/// <see cref="PasswordHasher{TUser}"/>, the framework implementation, rather than a hand-rolled
/// PBKDF2: the identity contract says to use the framework's own services for password hashing and
/// security stamps.
/// </para>
/// <para>
/// <b>Three shapes in one column.</b> 3,867 values are encrypted once, 4 were encrypted twice by a
/// known defect in the legacy application, and 3 were never encrypted at all.
/// <c>LegacyCrypt.TryDecrypt</c> already handles all three — it unwraps repeatedly, returns a plain
/// value untouched, and returns <c>null</c> for ciphertext it cannot read.
/// </para>
/// <para>
/// <b>Plaintext never leaves this method.</b> It is not logged, not written to disk, not returned
/// in the step note, and not held after the hash is computed. The sample verification checks that
/// the hash validates against the plaintext and reports only a count.
/// </para>
/// <para>
/// <b>The flag stays up.</b> <c>MustChangePassword</c> is left as the tenancy step set it. These
/// passwords were readable by anyone with database access, which makes them weak by construction;
/// carrying them across is what lets people sign in, not a reason to trust them.
/// </para>
/// <para>
/// <b>Idempotent by inspection.</b> A user who already has a hash is skipped, so a second run
/// writes nothing and a run that dies halfway can simply be repeated.
/// </para>
/// </summary>
public sealed class PasswordStep : IMigrationStep
{
    public int Order => 25;

    public string Name => "passwords";

    public string Description => "Decrypts legacy passwords and stores them as ASP.NET Identity hashes";

    /// <summary>
    /// Rows per statement. Each carries three parameters and SQL Server accepts 2,100 in one
    /// request, so six hundred fits with room to spare.
    /// </summary>
    private const int BatchSize = 600;

    /// <summary>How many hashes to prove against their plaintext before it is discarded.</summary>
    private const int SampleSize = 50;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        var users = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        if (users.Count == 0)
        {
            return new StepResult(0, 0, 0, "no users in the id map — run the tenancy step first");
        }

        var withoutHash = await UsersWithoutHashAsync(context, cancellationToken);

        var hasher = new PasswordHasher<User>();
        var subject = new User();

        var read = 0;
        var unmapped = 0;
        var unreadable = 0;
        var alreadySet = 0;
        var verified = 0;
        var sampled = 0;
        var pending = new List<(int Id, string Hash, string Stamp)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT KullaniciId, Sifre FROM Kullanici_T WHERE Sifre IS NOT NULL AND LEN(Sifre) > 0",
            connection) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = reader.GetInt32(0);
                if (!users.TryGetValue(legacyId, out var userId))
                {
                    unmapped++;
                    continue;
                }

                if (!withoutHash.Contains(userId))
                {
                    alreadySet++;
                    continue;
                }

                var plaintext = LegacyCrypt.TryDecrypt(reader.GetString(1));
                if (string.IsNullOrEmpty(plaintext))
                {
                    // Ciphertext this key cannot read. Counted, never guessed at: writing a hash
                    // of the wrong text would lock the user out with no way back.
                    unreadable++;
                    continue;
                }

                var hash = hasher.HashPassword(subject, plaintext);

                // Proof, on a sample, that the hash really is the hash of this password — while
                // the plaintext is still in hand and before it goes out of scope. Only the count
                // survives this block.
                if (sampled < SampleSize)
                {
                    sampled++;
                    if (hasher.VerifyHashedPassword(subject, hash, plaintext) != PasswordVerificationResult.Failed)
                    {
                        verified++;
                    }
                }

                pending.Add((userId, hash, Guid.NewGuid().ToString("N")));
            }
        }

        if (context.DryRun)
        {
            return new StepResult(read, 0, unmapped + unreadable + alreadySet,
                $"dry run: {pending.Count} would be hashed, {unreadable} unreadable, "
                + $"{alreadySet} already set, {unmapped} unmapped");
        }

        var written = await WriteAsync(context, pending, cancellationToken);

        context.Logger.LogInformation(
            "    passwords: {Written} hashed, {Unreadable} unreadable, {AlreadySet} already set, "
            + "{Unmapped} unmapped, {Verified}/{Sampled} sample hashes verified",
            written, unreadable, alreadySet, unmapped, verified, sampled);

        var note = $"{written} hashed; {verified}/{sampled} sampled hashes verify";
        if (unreadable > 0)
        {
            note += $"; {unreadable} UNREADABLE, left without a password";
        }
        if (unmapped > 0)
        {
            note += $"; {unmapped} not in the id map";
        }

        return new StepResult(read, written, unmapped + unreadable + alreadySet, note);
    }

    /// <summary>
    /// The users that still have no password, read from the destination rather than assumed. This
    /// is what makes the step idempotent, and it is also what stops a re-run from overwriting a
    /// password somebody has since changed.
    /// </summary>
    private static async Task<HashSet<int>> UsersWithoutHashAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<int>();

        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand(
            "SELECT Id FROM ensa.[User] WHERE PasswordHash IS NULL OR LEN(PasswordHash) = 0",
            connection) { CommandTimeout = 600 };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }

    /// <summary>
    /// Writes the hashes set-based, and refreshes the security stamp with them: a changed password
    /// has to invalidate whatever the old one authorised.
    /// </summary>
    private static async Task<int> WriteAsync(
        MigrationContext context,
        List<(int Id, string Hash, string Stamp)> pending,
        CancellationToken cancellationToken)
    {
        if (pending.Count == 0)
        {
            return 0;
        }

        var written = 0;

        await using var connection = await context.OpenModernAsync(cancellationToken);

        foreach (var chunk in pending.Chunk(BatchSize))
        {
            var values = string.Join(",", chunk.Select((_, index) => $"(@i{index},@h{index},@s{index})"));

            await using var command = new SqlCommand(
                $"""
                 UPDATE target
                 SET PasswordHash = source.Hash,
                     SecurityStamp = source.Stamp
                 FROM ensa.[User] AS target
                 JOIN (VALUES {values}) AS source (Id, Hash, Stamp) ON target.Id = source.Id
                 WHERE target.PasswordHash IS NULL OR LEN(target.PasswordHash) = 0;
                 """, connection) { CommandTimeout = 1800 };

            for (var index = 0; index < chunk.Length; index++)
            {
                command.Parameters.AddWithValue($"@i{index}", chunk[index].Id);
                command.Parameters.AddWithValue($"@h{index}", chunk[index].Hash);
                command.Parameters.AddWithValue($"@s{index}", chunk[index].Stamp);
            }

            written += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return written;
    }
}
