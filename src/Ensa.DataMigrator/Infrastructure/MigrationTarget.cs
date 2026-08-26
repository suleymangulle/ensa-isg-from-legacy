using Microsoft.Data.SqlClient;

namespace Ensa.DataMigrator.Infrastructure;

/// <summary>
/// The two databases this tool touches, and the interlock that stops it touching the wrong one.
/// <para>
/// <b>Why an interlock.</b> Reading the legacy database is harmless; writing the modern one is
/// not. The development and production targets differ by three characters (<c>EnsaDbDEv</c> vs
/// <c>EnsaDb</c>), they live on the same server, and the same credentials reach both. A tool that
/// runs against whatever the configuration happens to say will eventually run against the wrong
/// one, and a data migration is not something you undo. So the caller has to name the database
/// out loud: <c>--confirm EnsaDbDEv</c>. If the name does not match what the connection string
/// actually resolves to, nothing runs.
/// </para>
/// </summary>
public sealed class MigrationTarget
{
    private MigrationTarget(string legacyConnectionString, string modernConnectionString,
                            string legacyDatabase, string modernDatabase, string server)
    {
        LegacyConnectionString = legacyConnectionString;
        ModernConnectionString = modernConnectionString;
        LegacyDatabase = legacyDatabase;
        ModernDatabase = modernDatabase;
        Server = server;
    }

    /// <summary>Source: the legacy application's database. Opened read-only by convention.</summary>
    public string LegacyConnectionString { get; }

    /// <summary>Destination: the rebuilt schema.</summary>
    public string ModernConnectionString { get; }

    public string LegacyDatabase { get; }

    public string ModernDatabase { get; }

    public string Server { get; }

    /// <summary>
    /// Resolves both connections and refuses unless <paramref name="confirmedDatabase"/> matches
    /// the destination the configuration actually points at.
    /// </summary>
    public static MigrationTarget Resolve(
        string? legacyConnectionString,
        string? modernConnectionString,
        string? confirmedDatabase)
    {
        if (string.IsNullOrWhiteSpace(legacyConnectionString))
        {
            throw new InvalidOperationException(
                "The legacy connection string is missing. Set ConnectionStrings:Legacy — put it in "
                + "src/Ensa.HttpApi.Host/appsettings.Development.local.json, which .gitignore "
                + "excludes; this repository is public and a committed credential is a published "
                + "credential.");
        }

        if (string.IsNullOrWhiteSpace(modernConnectionString))
        {
            throw new InvalidOperationException(
                "The destination connection string is missing (ConnectionStrings:Default).");
        }

        var legacy = new SqlConnectionStringBuilder(legacyConnectionString);
        var modern = new SqlConnectionStringBuilder(modernConnectionString);

        if (string.IsNullOrWhiteSpace(confirmedDatabase))
        {
            throw new InvalidOperationException(
                $"Refusing to run without --confirm. This would write to '{modern.InitialCatalog}' "
                + $"on '{modern.DataSource}', reading from '{legacy.InitialCatalog}'. "
                + $"Re-run with: --confirm {modern.InitialCatalog}");
        }

        if (!string.Equals(confirmedDatabase, modern.InitialCatalog, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run: --confirm says '{confirmedDatabase}' but the configuration "
                + $"resolves to '{modern.InitialCatalog}' on '{modern.DataSource}'. One of the two "
                + "is wrong, and guessing which is not this tool's job.");
        }

        if (string.Equals(legacy.InitialCatalog, modern.InitialCatalog, StringComparison.OrdinalIgnoreCase)
            && string.Equals(legacy.DataSource, modern.DataSource, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Refusing to run: the source and the destination are the same database.");
        }

        return new MigrationTarget(
            legacyConnectionString, modernConnectionString,
            legacy.InitialCatalog, modern.InitialCatalog, modern.DataSource);
    }
}
