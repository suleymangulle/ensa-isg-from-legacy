using Ensa.DbMigrator.Seeding;
using Ensa.Domain.Membership;
using Ensa.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// ---------------------------------------------------------------------
// Ensa.DbMigrator
// Applies the database schema (migrations) and loads the seed data.
// Usage:  dotnet run --project src/Ensa.DbMigrator
//         dotnet run --project src/Ensa.DbMigrator -- --new-encryption-key
// ---------------------------------------------------------------------

// Generating a key pair touches no database, so it runs before anything is wired up.
if (args.Contains("--new-encryption-key", StringComparer.OrdinalIgnoreCase))
{
    var key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    var iv = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);

    Console.WriteLine("A fresh AES-256 key and IV for the encrypted columns.");
    Console.WriteLine();
    Console.WriteLine("  Encryption__Key=" + Convert.ToBase64String(key));
    Console.WriteLine("  Encryption__Iv=" + Convert.ToBase64String(iv));
    Console.WriteLine();
    Console.WriteLine("Set them as environment variables, user-secrets or key-vault entries -");
    Console.WriteLine("NOT in appsettings.json, which is in source control.");
    Console.WriteLine("Changing the key on an existing database makes its encrypted columns");
    Console.WriteLine("unreadable; rotating requires a re-encryption migration.");
    return 0;
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting the Ensa database migrator...");

    // The migrator reads HttpApi.Host's settings files, so it has to resolve the environment the
    // same way the host does. A generic host looks only at DOTNET_ENVIRONMENT; ASP.NET Core also
    // honours ASPNETCORE_ENVIRONMENT, which is the one a developer machine actually sets. Without
    // this the migrator falls back to Production, reads appsettings.json instead of
    // appsettings.Development.json, and tries to migrate a database on a server that is not there.
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        EnvironmentName =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    });

    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);

    // The connection string is read from the same files as HttpApi.Host — one source of truth.
    var hostSettingsDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Ensa.HttpApi.Host"));

    if (Directory.Exists(hostSettingsDirectory))
    {
        builder.Configuration
            .AddJsonFile(Path.Combine(hostSettingsDirectory, "appsettings.json"), optional: true)
            .AddJsonFile(
                Path.Combine(hostSettingsDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"),
                optional: true)
            // Local overrides, loaded last so they win. Kept out of source control by .gitignore:
            // this repository is public and a committed connection string is a published one.
            .AddJsonFile(
                Path.Combine(
                    hostSettingsDirectory,
                    $"appsettings.{builder.Environment.EnvironmentName}.local.json"),
                optional: true);
    }

    builder.Configuration.AddEnvironmentVariables();

    builder.Services.AddSerilog();
    builder.Services.AddEnsaEntityFrameworkCore(builder.Configuration);

    // The seeders use UserManager/RoleManager; the full Identity stack is not needed.
    builder.Services
        .AddIdentityCore<User>(o =>
        {
            o.Password.RequiredLength = 8;
            o.Password.RequireDigit = true;
            o.Password.RequireUppercase = true;
            o.Password.RequireNonAlphanumeric = false;
            o.User.RequireUniqueEmail = false;
        })
        .AddRoles<Role>()
        .AddEntityFrameworkStores<EnsaDbContext>();

    // OpenIddict's core services, so the seeder can register the first-party client and the API
    // scope through the managers rather than writing to the tables by hand. Only AddCore: this
    // tool issues no tokens and validates none, so the server and validation halves would be
    // dead weight.
    builder.Services
        .AddOpenIddict()
        .AddCore(options => options
            .UseEntityFrameworkCore()
            .UseDbContext<EnsaDbContext>()
            .ReplaceDefaultEntities<int>());

    builder.Services.AddScoped<IDataSeeder, ReferenceSeeder>();
    builder.Services.AddScoped<IDataSeeder, DistrictSeeder>();
    builder.Services.AddScoped<IDataSeeder, OpenIddictSeeder>();
    builder.Services.AddScoped<IDataSeeder, AuthorizationSeeder>();
    builder.Services.AddScoped<IDataSeeder, PermissionEndpointSeeder>();
    builder.Services.AddScoped<IDataSeeder, MenuSeeder>();
    builder.Services.AddScoped<IDataSeeder, MembershipSeeder>();

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILogger<Program>>();

    // ---- 1) Migration ----
    var context = sp.GetRequiredService<EnsaDbContext>();

    // When the database does not exist yet, the history table cannot be read, so the pending
    // list comes back empty and the migration would be skipped on a fresh machine. Ask the
    // connection first and treat "not there" as "everything is pending".
    var databaseExists = await context.Database.CanConnectAsync();

    var pending = databaseExists
        ? (await context.Database.GetPendingMigrationsAsync()).ToList()
        : context.Database.GetMigrations().ToList();

    if (pending.Count == 0)
    {
        logger.LogInformation("No pending migrations; the schema is up to date.");
    }
    else
    {
        logger.LogInformation(
            "Applying {Count} migration(s): {Migrations}",
            pending.Count,
            string.Join(", ", pending));

        await context.Database.MigrateAsync();
        logger.LogInformation("Migrations applied.");
    }

    // ---- 2) Seed ----
    var seeders = sp.GetServices<IDataSeeder>().OrderBy(s => s.Order).ToList();

    foreach (var seeder in seeders)
    {
        logger.LogInformation("Seeding: {Seeder}", seeder.Name);
        await seeder.SeedAsync();
    }

    // ---- 3) Optional maintenance ----
    // OpenIddict accumulates a row per token and per authorization, and they stay after they
    // expire: the framework's own PruneAsync is what clears them, so this uses that rather than
    // deleting from the tables by hand. Behind a flag because it removes data - an expired token
    // is harmless, and a run that quietly deletes rows is not what a migrator should do.
    if (args.Contains("--prune-openiddict", StringComparer.OrdinalIgnoreCase))
    {
        var threshold = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1);

        var tokens = await sp.GetRequiredService<OpenIddict.Abstractions.IOpenIddictTokenManager>()
            .PruneAsync(threshold);
        var authorizations = await sp
            .GetRequiredService<OpenIddict.Abstractions.IOpenIddictAuthorizationManager>()
            .PruneAsync(threshold);

        logger.LogInformation(
            "OpenIddict pruned: {Tokens} token(s), {Authorizations} authorization(s).",
            tokens, authorizations);
    }

    logger.LogInformation("Done. {Count} seeder(s) executed.", seeders.Count);
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "The database migration run failed.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
