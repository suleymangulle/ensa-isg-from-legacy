using Ensa.Domain.Membership;
using Ensa.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// Fills the endpoint-to-permission table from <see cref="PermissionEndpointSeedData"/>.
/// <para>
/// This is the table that lets a controller stop knowing which permission guards it. Until it is
/// populated, every guarded endpoint is refused — which is the correct failure: an authorization
/// table that is half seeded must lock people out rather than let them through.
/// </para>
/// <para>
/// Runs after the permission catalogue, because it resolves each entry's permission by target name
/// and needs those rows to exist. An entry whose permission is missing is reported and skipped
/// rather than written with a null permission, because null in that column means "deliberately
/// open" and inventing that for a lookup failure would quietly unguard an endpoint.
/// </para>
/// </summary>
public sealed class PermissionEndpointSeeder(
    EnsaDbContext context,
    ILogger<PermissionEndpointSeeder> logger) : IDataSeeder
{
    public int Order => 150;

    public string Name => "Endpoint permission map";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await context.Set<Permission>()
            .Where(x => x.PermissionTarget != null)
            .ToDictionaryAsync(x => x.PermissionTarget!, x => x.Id, StringComparer.Ordinal, cancellationToken);

        var existing = await context.Set<PermissionEndpoint>()
            .ToDictionaryAsync(x => (x.ControllerName, x.ActionName), cancellationToken);

        var added = 0;
        var updated = 0;
        var unknown = new List<string>();

        foreach (var entry in PermissionEndpointSeedData.All)
        {
            int? permissionId = null;

            if (entry.PermissionTarget is { } target)
            {
                if (!permissions.TryGetValue(target, out var id))
                {
                    unknown.Add($"{entry.Controller}.{entry.Action} -> {target}");
                    continue;
                }

                permissionId = id;
            }

            if (existing.TryGetValue((entry.Controller, entry.Action), out var row))
            {
                if (row.PermissionId != permissionId)
                {
                    row.PermissionId = permissionId;
                    updated++;
                }

                continue;
            }

            context.Add(new PermissionEndpoint
            {
                ControllerName = entry.Controller,
                ActionName = entry.Action,
                PermissionId = permissionId,
            });

            added++;
        }

        // The seed file is the whole truth for this table. A row it no longer lists is an endpoint
        // that no longer exists, or one whose name changed -- leaving it behind would mean a stale
        // mapping silently guarding nothing, which is exactly the kind of rot an authorization
        // table must not accumulate.
        var wanted = PermissionEndpointSeedData.All
            .Select(entry => (entry.Controller, entry.Action))
            .ToHashSet();

        var stale = existing
            .Where(pair => !wanted.Contains(pair.Key))
            .Select(pair => pair.Value)
            .ToList();

        if (stale.Count > 0)
        {
            context.RemoveRange(stale);
            logger.LogInformation("  endpoints: {Stale} stale row(s) removed", stale.Count);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "  endpoints: {Added} added, {Updated} updated, {Total} in the map",
            added, updated, PermissionEndpointSeedData.All.Length - unknown.Count);

        foreach (var missing in unknown)
        {
            logger.LogError("  endpoint skipped, its permission does not exist: {Entry}", missing);
        }
    }
}
