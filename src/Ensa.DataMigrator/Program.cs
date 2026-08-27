using Ensa.DataMigrator.Infrastructure;
using Ensa.EntityFrameworkCore.ValueConverters;
using Ensa.DataMigrator.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// ---------------------------------------------------------------------
// Ensa.DataMigrator
// Carries the legacy application's data into the rebuilt schema.
//
//   dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv
//   dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv --dry-run
//   dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv --step locations
//   dotnet run --project src/Ensa.DataMigrator -- --list
//
// --confirm is not a formality. The development and production databases differ by three
// characters, sit on the same server and answer to the same credentials; naming the destination
// out loud is what stops this tool from writing to the wrong one.
// ---------------------------------------------------------------------

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        EnvironmentName =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    });

    // Same settings files as the API and the schema migrator, local overrides last. The legacy
    // connection string carries a password, so it lives only in appsettings.*.local.json.
    var hostSettings = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Ensa.HttpApi.Host"));

    if (Directory.Exists(hostSettings))
    {
        builder.Configuration
            .AddJsonFile(Path.Combine(hostSettings, "appsettings.json"), optional: true)
            .AddJsonFile(
                Path.Combine(hostSettings, $"appsettings.{builder.Environment.EnvironmentName}.json"),
                optional: true)
            .AddJsonFile(
                Path.Combine(hostSettings, $"appsettings.{builder.Environment.EnvironmentName}.local.json"),
                optional: true);
    }

    builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
    builder.Services.AddSerilog();

    var steps = new IMigrationStep[]
    {
        new LocationStep(),
        new CatalogueStep(),
        new TenancyStep(),
        new PasswordStep(),
        new UserSplitStep(),
        new CompanyStep(),
        new OperationsStep(),
        new VisitStep(),
        new PlanStep(),
        new RiskStep(),
        new HealthStep(),
        new ReencryptStep(),
        new UserIdentityVerifyStep(),
        new UserColumnClassifyStep(),
        new VerifyStep(),
    };

    if (args.Contains("--list", StringComparer.OrdinalIgnoreCase))
    {
        Log.Information("Steps, in order:");
        foreach (var step in steps.OrderBy(s => s.Order))
        {
            Log.Information("  {Order:D2} {Name,-16} {Description}", step.Order, step.Name, step.Description);
        }

        return 0;
    }

    // Column encryption is a process-wide static that EF model building reads, normally set by
    // AddEnsaEntityFrameworkCore. This tool builds its DbContext by hand, so without this the
    // converter falls back to the published development key and everything written to an encrypted
    // column is unreadable by the application - silently, because the migrator would then read it
    // back with the same wrong key and find it perfectly healthy.
    var encryption = new EnsaEncryptionOptions();
    builder.Configuration.GetSection(EnsaEncryptionOptions.SectionName).Bind(encryption);
    encryption.EnsureUsable(builder.Environment.EnvironmentName);
    EnsaEncryptionOptions.SetCurrent(encryption);

    Log.Information(
        "Column encryption: {State}",
        encryption.IsConfigured ? "configured" : "development fallback");

    var target = MigrationTarget.Resolve(
        builder.Configuration.GetConnectionString("Legacy"),
        builder.Configuration.GetConnectionString("Default"),
        Value(args, "--confirm"));

    // Reading the legacy ciphertext is the one part of this tool that cannot be checked by
    // inspection: either the key, the block size and the padding are all right and identity numbers
    // come out, or something is subtly wrong and plausible rubbish comes out. --probe-crypt decrypts
    // a sample and says which.
    if (args.Contains("--probe-crypt", StringComparer.OrdinalIgnoreCase))
    {
        return await ProbeLegacyCryptAsync(target.LegacyConnectionString);
    }

    // Whether a migrated user can actually sign in. The sample check inside the password step
    // proves a hash matches its plaintext; this proves the whole round trip against the running
    // API, which is the only thing that answers "can these people log in".
    if (args.Contains("--probe-login", StringComparer.OrdinalIgnoreCase))
    {
        return await ProbeLoginAsync(
            target.LegacyConnectionString,
            target.ModernConnectionString,
            Values(args, "--api").FirstOrDefault() ?? "https://localhost:7001");
    }

    var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
    var only = Values(args, "--step");

    using var host = builder.Build();
    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

    var idMap = new IdMap(target.ModernConnectionString);
    await idMap.EnsureCreatedAsync();

    var context = new MigrationContext(
        target, idMap, loggerFactory.CreateLogger("Ensa.DataMigrator"), dryRun);

    // Column limits come from the destination itself, so they cannot drift from the schema; the
    // encrypted ones are then corrected to their plaintext capacity, from the EF model.
    await context.Fitter.LoadAsync(target.ModernConnectionString);

    await using (var model = context.CreateDbContext())
    {
        context.Fitter.ApplyEncryptedColumnLimits(model);
    }

    var runner = new MigrationRunner(steps, context, loggerFactory.CreateLogger<MigrationRunner>());

    return await runner.RunAsync(only);
}
catch (Exception exception)
{
    Log.Fatal(exception, "The data migration run failed.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Decrypts a sample of legacy values and reports whether they came out as identity numbers.
/// </summary>
static async Task<int> ProbeLegacyCryptAsync(string legacyConnectionString)
{
    await using var connection = new Microsoft.Data.SqlClient.SqlConnection(legacyConnectionString);
    await connection.OpenAsync();

    await using var command = new Microsoft.Data.SqlClient.SqlCommand(
        "SELECT TOP 200 TCKimlikNo FROM Kullanici_T WHERE TCKimlikNo IS NOT NULL", connection);

    var encrypted = 0;
    var eleven = 0;
    var failed = 0;
    var plain = 0;
    var odd = new List<string>();
    string? example = null;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var value = reader.GetString(0);

        if (!LegacyCrypt.LooksEncrypted(value))
        {
            plain++;
            continue;
        }

        encrypted++;
        var decrypted = LegacyCrypt.TryDecrypt(value);

        if (decrypted is null)
        {
            failed++;
            continue;
        }

        if (decrypted.Length == 11 && decrypted.All(char.IsDigit))
        {
            eleven++;
            example ??= decrypted[..3] + "********";
        }
        else
        {
            odd.Add($"{decrypted.Length} char(s)");
        }
    }

    Log.Information("Legacy ciphertext prefix : {Prefix}", LegacyCrypt.CipherPrefix);
    Log.Information("sampled                  : {Total}", encrypted + plain);
    Log.Information("  already plain          : {Plain}", plain);
    Log.Information("  encrypted              : {Encrypted}", encrypted);
    Log.Information("  decrypted to 11 digits : {Eleven}", eleven);
    Log.Information("  would not decrypt      : {Failed}", failed);
    Log.Information("  example                : {Example}", example ?? "(none)");

    if (odd.Count > 0)
    {
        Log.Information("  decrypted, not 11 digits: {Odd}", string.Join(", ", odd.Take(10)));
    }

    // Two separate questions. Whether the DECRYPTION works is answered by "nothing failed and the
    // output is well-formed text" - eleven digits appearing at all proves the key, the block size
    // and the padding are right together, because no near-miss produces that. Whether the SOURCE
    // is clean is a different question: a field somebody typed a passport number into decrypts
    // perfectly and is still not an identity number, and that is not the decryption's fault.
    var trustworthy = encrypted > 0 && failed == 0 && eleven > encrypted * 0.9;

    Log.Information(trustworthy
        ? "The decryption is trustworthy: nothing failed and {Eleven}/{Encrypted} are well-formed "
          + "identity numbers. The rest decrypted cleanly into something else, which is the source "
          + "data, not the key."
        : "NOT trustworthy - do not migrate encrypted columns until this reads clean.",
        eleven, encrypted);

    return trustworthy ? 0 : 1;
}

static string? Value(string[] args, string name)
{
    var index = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static List<string> Values(string[] args, string name)
{
    var found = new List<string>();
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            found.Add(args[index + 1]);
        }
    }

    return found;
}

/// <summary>
/// Signs a migrated user in against the running API, to prove the password migration end to end.
/// <para>
/// The sample check inside <c>PasswordStep</c> proves a hash matches the plaintext it was made
/// from. That is not the same as proving the application accepts it: the hash format, the security
/// stamp, the user lookup and the token endpoint all sit between the two. This walks the whole path.
/// </para>
/// <para>
/// <b>No password is printed.</b> The plaintext is decrypted, posted and dropped; only the user
/// name and the HTTP status come out.
/// </para>
/// </summary>
static async Task<int> ProbeLoginAsync(string legacyConnectionString, string modernConnectionString, string api)
{
    const int Sample = 5;

    // Legacy id -> modern user, for users that actually received a hash.
    var candidates = new List<(int LegacyId, string UserName)>();

    await using (var modern = new Microsoft.Data.SqlClient.SqlConnection(modernConnectionString))
    {
        await modern.OpenAsync();
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(
            $"""
             SELECT TOP {Sample} m.LegacyId, u.UserName
             FROM migration.IdMap AS m
             JOIN ensa.[User] AS u ON u.Id = m.ModernId
             JOIN ensa.UserProfile AS p ON p.UserId = u.Id
             WHERE m.LegacyTable = 'Kullanici_T'
               AND u.PasswordHash IS NOT NULL AND LEN(u.PasswordHash) > 0
               AND p.IsActive = 1 AND p.IsDeleted = 0
             ORDER BY m.LegacyId;
             """, modern);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            candidates.Add((reader.GetInt32(0), reader.GetString(1)));
        }
    }

    if (candidates.Count == 0)
    {
        Log.Error("No migrated user has a password hash. Run the passwords step first.");
        return 1;
    }

    var secrets = new Dictionary<int, string>();

    await using (var legacy = new Microsoft.Data.SqlClient.SqlConnection(legacyConnectionString))
    {
        await legacy.OpenAsync();
        var ids = string.Join(",", candidates.Select(c => c.LegacyId));
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(
            $"SELECT KullaniciId, Sifre FROM Kullanici_T WHERE KullaniciId IN ({ids})", legacy);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var plaintext = LegacyCrypt.TryDecrypt(reader.GetString(1));
            if (!string.IsNullOrEmpty(plaintext))
            {
                secrets[reader.GetInt32(0)] = plaintext;
            }
        }
    }

    // The development certificate is self-signed; this probe is a local diagnostic, never a
    // component of the application.
    using var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    };
    using var client = new HttpClient(handler) { BaseAddress = new Uri(api) };

    var succeeded = 0;
    var attempted = 0;

    foreach (var (legacyId, userName) in candidates)
    {
        if (!secrets.TryGetValue(legacyId, out var password))
        {
            continue;
        }

        attempted++;

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "ensa-spa",
            ["username"] = userName,
            ["password"] = password,
            ["scope"] = "openid profile email roles offline_access ensa",
        });

        try
        {
            using var response = await client.PostAsync("/connect/token", form);
            var ok = response.IsSuccessStatusCode;
            if (ok)
            {
                succeeded++;
            }

            Log.Information(
                "  {UserName,-28} HTTP {Status} {Verdict}",
                userName, (int)response.StatusCode, ok ? "SIGNED IN" : "REJECTED");
        }
        catch (HttpRequestException exception)
        {
            Log.Error("  {UserName,-28} could not reach {Api}: {Message}", userName, api, exception.Message);
            return 1;
        }
    }

    Log.Information("Migrated users signed in: {Succeeded}/{Attempted}", succeeded, attempted);
    return succeeded == attempted && attempted > 0 ? 0 : 1;
}
