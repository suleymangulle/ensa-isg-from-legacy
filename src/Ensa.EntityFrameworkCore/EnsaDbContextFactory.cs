using System.Text.Json;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.Ambient;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ensa.EntityFrameworkCore;

/// <summary>
/// Design-time factory that lets the <c>dotnet ef</c> tools (creating and applying migrations) build a
/// <see cref="EnsaDbContext"/> instance.
/// <para>
/// It is <b>not used</b> at runtime — the application obtains the context from DI.
/// This class only comes into play for <c>Add-Migration</c> / <c>dotnet ef migrations add</c>.
/// </para>
/// <para>
/// <b>Why read the JSON by hand?</b> The <c>Microsoft.Extensions.Configuration.Json</c> package is not in
/// this project's dependency graph (keeping the EF layer free of web/host packages is a deliberate layering
/// decision). <see cref="System.Text.Json"/> is part of the base framework, so it does the same job without
/// adding a dependency.
/// </para>
/// </summary>
public sealed class EnsaDbContextFactory : IDesignTimeDbContextFactory<EnsaDbContext>
{
    /// <summary>Name of the connection string inside <c>appsettings.json</c>.</summary>
    private const string ConnectionStringName = "Default";

    /// <summary>Name used to read the connection string from an environment variable.</summary>
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__Default";

    /// <summary>Local development connection used when no configuration is found.</summary>
    private const string FallbackConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=Ensa;Trusted_Connection=True;TrustServerCertificate=True";

    /// <inheritdoc />
    public EnsaDbContext CreateDbContext(string[] args)
    {
        var appSettingsPath = FindAppSettingsFile();
        var connectionString = ResolveConnectionString(appSettingsPath);

        // Encrypted column converters need the key while the model is being built.
        // Even without a real key at design time the option must be set, otherwise the model
        // schema (column lengths) would be generated inconsistently.
        ApplyEncryptionOptions(appSettingsPath);

        var optionsBuilder = new DbContextOptionsBuilder<EnsaDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EnsaMigrationsHistory", EnsaDomainSharedConsts.DbSchema));

        // At design time there is no HTTP context and no DI, so we work with null objects.
        // This is safe because global filters do not affect the migration output.
        return new EnsaDbContext(
            optionsBuilder.Options,
            NullCurrentTenant.Instance,
            NullCurrentUser.Instance,
            Clock.Instance,
            DataFilter.Instance);
    }

    /// <summary>
    /// Resolves the connection string in the following order:
    /// 1) the <c>ConnectionStrings__Default</c> environment variable,
    /// 2) <c>Ensa.HttpApi.Host\appsettings.json</c>,
    /// 3) the local development default.
    /// </summary>
    private static string ResolveConnectionString(string? appSettingsPath)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        if (appSettingsPath is not null)
        {
            var fromFile = ReadJsonValue(appSettingsPath, "ConnectionStrings", ConnectionStringName);
            if (!string.IsNullOrWhiteSpace(fromFile))
            {
                return fromFile;
            }
        }

        Console.WriteLine(
            $"[EnsaDbContextFactory] 'ConnectionStrings:{ConnectionStringName}' was not found; " +
            "falling back to the local development connection.");

        return FallbackConnectionString;
    }

    /// <summary>Reads the encryption options from <c>appsettings.json</c> and applies them process-wide.</summary>
    private static void ApplyEncryptionOptions(string? appSettingsPath)
    {
        var options = new EnsaEncryptionOptions
        {
            Key = Environment.GetEnvironmentVariable("Encryption__Key") ?? string.Empty,
            Iv = Environment.GetEnvironmentVariable("Encryption__Iv") ?? string.Empty
        };

        if (appSettingsPath is not null)
        {
            if (string.IsNullOrWhiteSpace(options.Key))
            {
                options.Key = ReadJsonValue(appSettingsPath, EnsaEncryptionOptions.SectionName, "Key") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(options.Iv))
            {
                options.Iv = ReadJsonValue(appSettingsPath, EnsaEncryptionOptions.SectionName, "Iv") ?? string.Empty;
            }
        }

        EnsaEncryptionOptions.SetCurrent(options);
    }

    /// <summary>
    /// Locates the <c>Ensa.HttpApi.Host\appsettings.json</c> file.
    /// <para>
    /// <c>dotnet ef</c> uses a different working directory depending on where it was started from
    /// (solution root, EF project, Host project), so several candidates are tried and, as a last
    /// resort, the directory tree is searched upwards.
    /// </para>
    /// </summary>
    private static string? FindAppSettingsFile()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        string[] candidates =
        [
            Path.Combine(currentDirectory, "appsettings.json"),
            Path.Combine(currentDirectory, "..", "Ensa.HttpApi.Host", "appsettings.json"),
            Path.Combine(currentDirectory, "src", "Ensa.HttpApi.Host", "appsettings.json"),
            Path.Combine(currentDirectory, "..", "..", "src", "Ensa.HttpApi.Host", "appsettings.json")
        ];

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (File.Exists(full))
            {
                return full;
            }
        }

        // Search upwards through the directory tree for "src\Ensa.HttpApi.Host\appsettings.json".
        var directory = new DirectoryInfo(currentDirectory);
        while (directory is not null)
        {
            var probe = Path.Combine(directory.FullName, "src", "Ensa.HttpApi.Host", "appsettings.json");
            if (File.Exists(probe))
            {
                return probe;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>Reads the <c>section:key</c> value from a JSON file; <c>null</c> when not found.</summary>
    private static string? ReadJsonValue(string path, string section, string key)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(section, out var sectionElement) ||
                sectionElement.ValueKind != JsonValueKind.Object ||
                !sectionElement.TryGetProperty(key, out var valueElement) ||
                valueElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return valueElement.GetString();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.WriteLine($"[EnsaDbContextFactory] '{path}' could not be read: {exception.Message}");
            return null;
        }
    }
}
