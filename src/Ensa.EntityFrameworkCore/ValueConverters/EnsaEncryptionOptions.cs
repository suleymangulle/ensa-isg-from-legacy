using System.Security.Cryptography;
using System.Text;

namespace Ensa.EntityFrameworkCore.ValueConverters;

/// <summary>
/// Column-level encryption options. Bound from the <c>"Encryption"</c> section of
/// <c>appsettings.json</c>:
/// <code>
/// "Encryption": {
///   "Key": "&lt;base64 32 bytes OR a free-form passphrase&gt;",
///   "Iv":  "&lt;base64 16 bytes OR free-form text&gt;"
/// }
/// </code>
/// <para>
/// <b>SECURITY:</b> the key is never kept in source control. In production an environment variable
/// (<c>Encryption__Key</c>) or a secret store (Key Vault, user-secrets) must be used. Changing the key makes
/// the existing encrypted columns unreadable; rotating it requires a separate re-encryption data migration.
/// </para>
/// </summary>
public sealed class EnsaEncryptionOptions
{
    /// <summary><c>appsettings.json</c> section name.</summary>
    public const string SectionName = "Encryption";

    /// <summary>
    /// AES-256 key. A Base64-encoded 32-byte value is used as is; any other text is hashed with SHA-256
    /// to derive 32 bytes.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// AES initialisation vector (IV). A Base64-encoded 16-byte value is used as is; any other text is
    /// hashed with SHA-256 and its first 16 bytes are taken.
    /// <para>
    /// <b>CAUTION — deliberate design decision:</b> the IV is <b>fixed</b>. This makes the encryption
    /// <i>deterministic</i> (the same plaintext always yields the same ciphertext), which is what allows
    /// equality queries (<c>WHERE NationalId = @p</c>), unique indexes and JOINs on encrypted columns.
    /// The price is that two rows holding the same value look identical in the database (the well-known
    /// leak of deterministic encryption). The legacy <c>[EncryptColumn]</c> behaviour was deterministic
    /// too; the same model is kept in order to preserve queryability.
    /// </para>
    /// </summary>
    public string Iv { get; set; } = string.Empty;

    /// <summary>
    /// Development value used when no key/IV is configured.
    /// For design time (migration generation) and local development only.
    /// <para>
    /// It is a <b>fixed, published</b> string, which is exactly why <see cref="EnsureUsable"/>
    /// refuses it outside Development: a deployment that silently starts on this value encrypts
    /// statutory identity numbers with a key anybody can read in this file.
    /// </para>
    /// </summary>
    private const string DevelopmentFallback = "Ensa-Development-Only-DO-NOT-USE-IN-PRODUCTION";

    /// <summary>Whether a real key and IV have been configured.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Key) && !string.IsNullOrWhiteSpace(Iv);

    /// <summary>
    /// Throws unless a real key is configured, so an environment that is not Development cannot
    /// start on the development fallback.
    /// <para>
    /// Failing at start-up is the point. The alternative is an application that runs perfectly,
    /// stores every national id under a key published in the repository, and gives no sign of it
    /// until somebody reads the source.
    /// </para>
    /// </summary>
    /// <param name="environmentName">The host environment name, e.g. <c>Production</c>.</param>
    public void EnsureUsable(string? environmentName)
    {
        if (IsConfigured)
        {
            return;
        }

        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Column encryption is not configured and the environment is '{environmentName ?? "(unset)"}'."
            + Environment.NewLine
            + Environment.NewLine
            + "If you are running this on a developer machine, the environment is the problem, not "
            + "the key: it should be Development, which supplies both a local connection string "
            + "and a development key. Visual Studio picks that up from the project's launch "
            + "profile; from a plain shell, set it first: "
            + "DOTNET_ENVIRONMENT=Development (or ASPNETCORE_ENVIRONMENT=Development)."
            + Environment.NewLine
            + Environment.NewLine
            + "If this IS a real deployment, supply a key: set Encryption__Key (32 bytes, base64) "
            + "and Encryption__Iv (16 bytes, base64) as environment variables, user-secrets or "
            + "key-vault entries. They are deliberately absent from appsettings.json - a key in "
            + "source control is a published key, and these columns hold statutory identity "
            + "numbers. Generate a pair with "
            + "'dotnet run --project src/Ensa.DbMigrator -- --new-encryption-key'. Changing the "
            + "key makes existing encrypted columns unreadable.");
    }

    /// <summary>
    /// Process-wide option used while the model is being built (IEntityTypeConfiguration).
    /// <para>
    /// Because EF Core model building is independent of the DI scope and happens once per process, the
    /// parameterless constructor of <see cref="EncryptedStringConverter"/> reads this static value. The value
    /// is assigned through <see cref="SetCurrent"/> by <c>AddEnsaEntityFrameworkCore</c> and
    /// <c>EnsaDbContextFactory</c>.
    /// </para>
    /// </summary>
    public static EnsaEncryptionOptions Current { get; private set; } = new();

    /// <summary>Sets the process-wide encryption options.</summary>
    public static void SetCurrent(EnsaEncryptionOptions options)
        => Current = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>Derives a 32-byte AES-256 key from the configured key.</summary>
    public byte[] ResolveKey() => DeriveBytes(Key, 32);

    /// <summary>Derives a 16-byte AES block from the configured IV.</summary>
    public byte[] ResolveIv() => DeriveBytes(Iv, 16);

    private static byte[] DeriveBytes(string value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            value = DevelopmentFallback;
        }

        // If it is Base64 and has the exact length, use it directly.
        if (TryDecodeBase64(value, out var raw) && raw.Length == length)
        {
            return raw;
        }

        // Otherwise derive it deterministically.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        if (hash.Length == length)
        {
            return hash;
        }

        var result = new byte[length];
        Array.Copy(hash, result, Math.Min(hash.Length, length));
        return result;
    }

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        var buffer = new byte[((value.Length * 3) + 3) / 4];
        if (Convert.TryFromBase64String(value, buffer, out var written))
        {
            bytes = buffer[..written];
            return true;
        }

        bytes = [];
        return false;
    }
}
