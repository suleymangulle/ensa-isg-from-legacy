using Ensa.DataMigrator.Infrastructure;
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

    var target = MigrationTarget.Resolve(
        builder.Configuration.GetConnectionString("Legacy"),
        builder.Configuration.GetConnectionString("Default"),
        Value(args, "--confirm"));

    var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
    var only = Values(args, "--step");

    using var host = builder.Build();
    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

    var idMap = new IdMap(target.ModernConnectionString);
    await idMap.EnsureCreatedAsync();

    var context = new MigrationContext(
        target, idMap, loggerFactory.CreateLogger("Ensa.DataMigrator"), dryRun);

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
