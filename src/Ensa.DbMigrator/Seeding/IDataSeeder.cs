namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// Seed data loader. Every implementation must be <b>idempotent</b>: running it repeatedly
/// against the same database must not produce duplicate rows.
/// </summary>
public interface IDataSeeder
{
    /// <summary>Execution order — the lowest value runs first.</summary>
    int Order { get; }

    /// <summary>Name shown in the logs.</summary>
    string Name { get; }

    Task SeedAsync(CancellationToken cancellationToken = default);
}
