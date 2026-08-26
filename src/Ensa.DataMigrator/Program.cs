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
        new CompanyStep(),
        new OperationsStep(),
        new VisitStep(),
        new PlanStep(),
        new ReencryptStep(),
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
