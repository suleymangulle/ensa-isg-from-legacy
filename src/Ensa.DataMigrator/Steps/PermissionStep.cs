using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Carries the legacy permission model across: the catalogue and every gate that references it.
/// <para>
/// <b>What was lost.</b> The permission catalogue was never migrated — it was hand-written and
/// seeded, 171 rows against the legacy 419, and the assignments that decide who actually holds
/// what came across incomplete or not at all: 5,069 scope links became none, 1,406 organization
/// type grants became 684, 1,410 plan grants became 855, 1,360 user type grants became 540. The
/// rules were ported faithfully into <c>PermissionManager</c>; the data they operate on was not.
/// </para>
/// <para>
/// <b>The shape is already right.</b> <c>Permission</c> was designed against <c>Yetki_T</c> —
/// parent, type, target, name, description, red message, restriction mode — and
/// <c>PermissionScope</c> says in its own documentation that its link type matches the legacy enum
/// one to one. Nothing here needs inventing; it needs carrying.
/// </para>
/// <para>
/// <b>Two levels.</b> 92 page permissions, each the parent of some of the 327 method permissions —
/// which is the same shape as a controller and its actions. The parent link is resolved in a second
/// pass, because a child can appear before its parent in id order.
/// </para>
/// <para>
/// <b>What this does not do.</b> It does not rebind the API's endpoints. Those still point at the
/// seeded catalogue through <c>PermissionEndpoint</c>, and pointing them at these rows instead
/// means matching 92 legacy page names and 327 legacy method signatures onto the rebuilt
/// application — a judgement per row, not a rule. That is reported, not guessed at.
/// </para>
/// </summary>
public sealed class PermissionStep : IMigrationStep
{
    public int Order => 28;

    public string Name => "permissions";

    public string Description => "Carries the 419 legacy permissions and the 9,640 grants that reference them";

    /// <summary>
    /// Legacy organization type code to the modern one. <c>ensa</c> is the software vendor's own
    /// type and has no counterpart in the rebuilt reference data; rows pointing at it are reported
    /// rather than assigned somewhere plausible.
    /// </summary>
    private static readonly (string Legacy, string Modern)[] OrganizationTypes =
    [
        ("Bireysel", "BIREYSEL"),
        ("Kurumsal", "ISGB"),
        ("OSGB", "OSGB"),
    ];

    /// <summary>Legacy plan code to the modern one. <c>ensa</c> is the vendor's own, as above.</summary>
    private static readonly (string Legacy, string Modern)[] SubscriptionPlans =
    [
        ("startup", "BASLANGIC"),
        ("pro", "PROFESYONEL"),
        ("demo", "DEMO"),
    ];

    /// <summary>
    /// Legacy staff type code to the modern one — the same mapping the user split uses, because it
    /// is the same fact.
    /// </summary>
    private static readonly (string Legacy, string Modern)[] UserTypes =
    [
        ("Uzman", "UZMAN"),
        ("Doktor", "HEKIM"),
        ("Admin", "KURUM-YONETICISI"),
        ("Diğer Sağlık", "DSP"),
        ("ofis-admin", "OFIS-YONETICISI"),
        ("ser-admin", "SISTEM-YONETICISI"),
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        var notes = new List<string>();

        var inserted = 0;
        var permissions = await CarryPermissionsAsync(context, notes, count => inserted = count, cancellationToken);

        if (permissions.Count == 0)
        {
            // A dry run reports what it counted; a real run with nothing to carry says so.
            return new StepResult(0, 0, 0,
                notes.Count > 0
                    ? string.Join("; ", notes)
                    : "no permissions carried; the gates were left alone");
        }

        // What this run actually wrote, not how big the map ended up: a re-run that inserts
        // nothing must say nothing, or the summary quietly claims work it did not do.
        var written = inserted;

        written += await CarryParentsAsync(context, permissions, notes, cancellationToken);
        written += await CarryScopesAsync(context, permissions, notes, cancellationToken);
        written += await CarryRestrictionsAsync(context, permissions, notes, cancellationToken);
        written += await CarryOrganizationTypesAsync(context, permissions, notes, cancellationToken);
        written += await CarryPlansAsync(context, permissions, notes, cancellationToken);
        written += await CarryUserTypesAsync(context, permissions, notes, cancellationToken);

        return new StepResult(written, written, 0, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ the catalogue

    private static async Task<Dictionary<int, int>> CarryPermissionsAsync(
        MigrationContext context,
        List<string> notes,
        Action<int> reportInserted,
        CancellationToken cancellationToken)
    {
        var map = await context.IdMap.LoadAsync("Yetki_T", cancellationToken);

        // Targets already in the destination, so a re-run matches rather than collides: the
        // column is unique, and the seeded catalogue is still sitting in the same table.
        var existingTargets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await using (var modern = await context.OpenModernAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT Id, PermissionTarget FROM ensa.Permission WHERE PermissionTarget IS NOT NULL", modern))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                existingTargets[reader.GetString(1)] = reader.GetInt32(0);
            }
        }

        var pending = new List<(int LegacyId, string Target, string Name, string? Description,
            string? Message, int Type, int RestrictionMode)>();
        var matched = new List<(int LegacyId, int ModernId)>();
        var read = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            """
            SELECT YetkiId, ISNULL(YetkiTuru,''), ISNULL(YetkiHedefi,''), ISNULL(YetkiAdi,''),
                   YetkiAciklamasi, Message, YetkiKisitHedef
            FROM Yetki_T
            ORDER BY YetkiId
            """, legacy) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = reader.GetInt32(0);
                var target = reader.GetString(2).Trim();

                if (target.Length == 0 || map.ContainsKey(legacyId))
                {
                    continue;
                }

                if (existingTargets.TryGetValue(target, out var already))
                {
                    // The seeded catalogue happens to name the same target. Matched, not inserted.
                    matched.Add((legacyId, already));
                    continue;
                }

                pending.Add((
                    legacyId,
                    target,
                    Fit(context, "Permission", "PermissionName", reader.GetString(3), target),
                    Fit(context, "Permission", "PermissionDescription", Text(reader, 4), null),
                    Fit(context, "Permission", "RedMessage", Text(reader, 5), null),
                    // sayfa-yetkisi -> PagePermission (1), method-yetkisi -> MethodPermission (2)
                    reader.GetString(1).StartsWith("method", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                    // null and "everybody" are Everyone (0); "only-selection" is OnlySelected (1).
                    string.Equals(Text(reader, 6), "only-selection", StringComparison.OrdinalIgnoreCase) ? 1 : 0));
            }
        }

        if (context.DryRun)
        {
            notes.Add($"dry run: {pending.Count} permissions would be inserted, {matched.Count} matched");
            return [];
        }

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        {
            foreach (var chunk in pending.Chunk(200))
            {
                var inserted = new List<(int, int)>();

                foreach (var row in chunk)
                {
                    await using var command = new SqlCommand(
                        """
                        INSERT INTO ensa.Permission
                            (PermissionType, PermissionTarget, PermissionName, PermissionDescription,
                             RedMessage, PermissionRestrictionMode, SortOrder, CreationTime)
                        OUTPUT INSERTED.Id
                        VALUES (@type, @target, @name, @description, @message, @mode, @sort, SYSDATETIME());
                        """, connection) { CommandTimeout = 600 };

                    command.Parameters.AddWithValue("@type", row.Type);
                    command.Parameters.AddWithValue("@target", row.Target);
                    command.Parameters.AddWithValue("@name", row.Name);
                    command.Parameters.AddWithValue("@description", (object?)row.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@message", (object?)row.Message ?? DBNull.Value);
                    command.Parameters.AddWithValue("@mode", row.RestrictionMode);
                    command.Parameters.AddWithValue("@sort", row.LegacyId);

                    inserted.Add((row.LegacyId, (int)(await command.ExecuteScalarAsync(cancellationToken))!));
                }

                // Written with the chunk that produced them: a run that dies halfway must not leave
                // permissions behind that nothing can find again.
                await context.IdMap.SaveAsync("Yetki_T", inserted, 'I', cancellationToken);
            }
        }

        if (matched.Count > 0)
        {
            await context.IdMap.SaveAsync("Yetki_T", matched, 'M', cancellationToken);
        }

        reportInserted(pending.Count);

        var result = await ReloadAsync(context, cancellationToken);

        context.Logger.LogInformation(
            "    permissions: {Inserted} inserted, {Matched} matched, {Total} in the map, of {Read} legacy rows",
            pending.Count, matched.Count, result.Count, read);

        notes.Add($"permissions: {pending.Count} inserted, {matched.Count} matched");
        return result;
    }

    /// <summary>
    /// The parent link, in a second pass. A method permission can appear before the page it belongs
    /// to, so the map has to be complete before any of them can be resolved.
    /// </summary>
    private static async Task<int> CarryParentsAsync(
        MigrationContext context,
        Dictionary<int, int> permissions,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var pending = new List<(int Id, int ParentId)>();
        var orphaned = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT YetkiId, ParentYetkiId FROM Yetki_T WHERE ParentYetkiId IS NOT NULL", legacy))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (permissions.TryGetValue(reader.GetInt32(0), out var id)
                    && permissions.TryGetValue(reader.GetInt32(1), out var parentId))
                {
                    pending.Add((id, parentId));
                }
                else
                {
                    orphaned++;
                }
            }
        }

        var written = await ApplyPairsAsync(
            context,
            "UPDATE target SET ParentPermissionId = source.ParentId FROM ensa.Permission AS target "
            + "JOIN (VALUES {0}) AS source (Id, ParentId) ON target.Id = source.Id "
            + "WHERE target.ParentPermissionId IS NULL",
            pending,
            cancellationToken);

        notes.Add($"parents: {written}" + (orphaned > 0 ? $", {orphaned} unresolvable" : string.Empty));
        return written;
    }

    // ------------------------------------------------------------------ the gates

    /// <summary>
    /// <c>YetkiBaglanti_T</c>, 5,069 rows and none of them carried before. This is what binds a
    /// permission to a module, a user type, an account or a menu.
    /// </summary>
    private static async Task<int> CarryScopesAsync(
        MigrationContext context,
        Dictionary<int, int> permissions,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var pending = new List<(int PermissionId, int LinkType, int? TargetId, string? Code, bool IsActive)>();
        var orphaned = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            """
            SELECT YetkiId, BaglantiType, BaglantiTypeId, BaglantiTypeString, ISNULL(Aktif, 1)
            FROM YetkiBaglanti_T
            """, legacy) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0) || !permissions.TryGetValue(reader.GetInt32(0), out var permissionId))
                {
                    orphaned++;
                    continue;
                }

                pending.Add((
                    permissionId,
                    reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    !reader.IsDBNull(4) && reader.GetBoolean(4)));
            }
        }

        var written = 0;

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        {
            foreach (var row in pending)
            {
                await using var command = new SqlCommand(
                    """
                    INSERT INTO ensa.PermissionScope
                        (LinkType, LinkTargetId, LinkTargetCode, PermissionId, IsActive, CreationTime)
                    SELECT @type, @targetId, @code, @permissionId, @active, SYSDATETIME()
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ensa.PermissionScope
                        WHERE PermissionId = @permissionId AND LinkType = @type
                          AND ISNULL(LinkTargetId, -1) = ISNULL(@targetId, -1)
                          AND ISNULL(LinkTargetCode, '') = ISNULL(@code, ''));
                    """, connection) { CommandTimeout = 1800 };

                command.Parameters.AddWithValue("@type", row.LinkType);
                command.Parameters.AddWithValue("@targetId", (object?)row.TargetId ?? DBNull.Value);
                command.Parameters.AddWithValue("@code", (object?)row.Code ?? DBNull.Value);
                command.Parameters.AddWithValue("@permissionId", row.PermissionId);
                command.Parameters.AddWithValue("@active", row.IsActive);

                written += await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        // Anything read but not written was already there: the source holds duplicate links, and
        // the insert refuses a second identical one.
        var duplicates = pending.Count - written;

        context.Logger.LogInformation(
            "    scopes: {Written} written, {Duplicates} duplicate, {Orphaned} orphaned, of {Total}",
            written, duplicates, orphaned, pending.Count + orphaned);

        notes.Add($"scopes: {written}"
                  + (duplicates > 0 ? $", {duplicates} duplicate in the source" : string.Empty)
                  + (orphaned > 0 ? $", {orphaned} orphaned" : string.Empty));
        return written;
    }

    /// <summary><c>YetkiKisit_T</c>: which user types a restricted permission is limited to.</summary>
    private static async Task<int> CarryRestrictionsAsync(
        MigrationContext context,
        Dictionary<int, int> permissions,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var userTypes = await CodeMapAsync(context, "UserType", cancellationToken);

        return await CarryGateAsync(
            context, notes, "restrictions",
            "SELECT YetkiId, KullaniciTypeId FROM YetkiKisit_T",
            permissions,
            legacyKey => legacyKey is int id && userTypes.Values.Contains(id) ? id : null,
            """
            INSERT INTO ensa.PermissionRestriction (PermissionId, UserTypeId, CreationTime)
            SELECT @permissionId, @target, SYSDATETIME()
            WHERE NOT EXISTS (SELECT 1 FROM ensa.PermissionRestriction
                              WHERE PermissionId = @permissionId AND UserTypeId = @target);
            """,
            cancellationToken);
    }

    private static async Task<int> CarryOrganizationTypesAsync(
        MigrationContext context,
        Dictionary<int, int> permissions,
        List<string> notes,
        CancellationToken cancellationToken)
        => await CarryCodedGateAsync(
            context, permissions, notes, "organization types",
            "SELECT y.YetkiId, t.KurumTuruKodu FROM KurumTuruYetki_T y "
            + "JOIN KurumTuru_T t ON t.KurumTuruId = y.KurumTuruId",
            OrganizationTypes, "OrganizationType",
            """
            INSERT INTO ensa.OrganizationTypePermission (OrganizationTypeId, PermissionId, CreationTime)
            SELECT @target, @permissionId, SYSDATETIME()
            WHERE NOT EXISTS (SELECT 1 FROM ensa.OrganizationTypePermission
                              WHERE OrganizationTypeId = @target AND PermissionId = @permissionId);
            """,
            cancellationToken);

    private static async Task<int> CarryPlansAsync(
        MigrationContext context,
        Dictionary<int, int> permissions,
        List<string> notes,
        CancellationToken cancellationToken)
        => await CarryCodedGateAsync(
            context, permissions, notes, "subscription plans",
            "SELECT y.YetkiId, t.PaketTuruKodu FROM PaketTuruYetki_T y "
            + "JOIN PaketTuru_T t ON t.PaketTuruId = y.PaketTuruId",
            SubscriptionPlans, "SubscriptionPlan",
            """
            INSERT INTO ensa.SubscriptionPlanPermission (SubscriptionPlanId, PermissionId, CreationTime)
            SELECT @target, @permissionId, SYSDATETIME()
            WHERE NOT EXISTS (SELECT 1 FROM ensa.SubscriptionPlanPermission
                              WHERE SubscriptionPlanId = @target AND PermissionId = @permissionId);
            """,
            cancellationToken);

    private static async Task<int> CarryUserTypesAsync(
        MigrationContext context,
        Dictionary<int, int> permissions,
        List<string> notes,
        CancellationToken cancellationToken)
        => await CarryCodedGateAsync(
            context, permissions, notes, "user types",
            "SELECT YetkiId, KullaniciTypeCode FROM KullaniciTypeYetki_T WHERE ISNULL(Aktif, 1) = 1",
            UserTypes, "UserType",
            """
            INSERT INTO ensa.UserTypePermission (UserTypeId, PermissionId, IsActive, CreationTime)
            SELECT @target, @permissionId, 1, SYSDATETIME()
            WHERE NOT EXISTS (SELECT 1 FROM ensa.UserTypePermission
                              WHERE UserTypeId = @target AND PermissionId = @permissionId);
            """,
            cancellationToken);

    // ------------------------------------------------------------------ plumbing

    /// <summary>A gate whose legacy side names its target by code.</summary>
    private static async Task<int> CarryCodedGateAsync(
        MigrationContext context,
        Dictionary<int, int> permissions,
        List<string> notes,
        string label,
        string legacySql,
        (string Legacy, string Modern)[] codes,
        string modernTable,
        string insertSql,
        CancellationToken cancellationToken)
    {
        var modern = await CodeMapAsync(context, modernTable, cancellationToken);

        var byLegacyCode = codes
            .Where(pair => modern.ContainsKey(pair.Modern))
            .ToDictionary(pair => pair.Legacy, pair => modern[pair.Modern], StringComparer.OrdinalIgnoreCase);

        var pending = new List<(int PermissionId, int TargetId)>();
        var unmapped = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var orphaned = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(legacySql, legacy) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1)
                    || !permissions.TryGetValue(reader.GetInt32(0), out var permissionId))
                {
                    // The grant names a permission that is not in Yetki_T at all. Broken
                    // referential integrity in the source, and counted rather than dropped
                    // quietly -- a grant nobody can trace is a grant nobody can audit.
                    orphaned++;
                    continue;
                }

                var code = reader.GetString(1).Trim();

                if (byLegacyCode.TryGetValue(code, out var targetId))
                {
                    pending.Add((permissionId, targetId));
                }
                else
                {
                    unmapped[code] = unmapped.GetValueOrDefault(code) + 1;
                }
            }
        }

        var written = 0;

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        {
            foreach (var (permissionId, targetId) in pending)
            {
                await using var command = new SqlCommand(insertSql, connection) { CommandTimeout = 1800 };
                command.Parameters.AddWithValue("@permissionId", permissionId);
                command.Parameters.AddWithValue("@target", targetId);

                written += await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        var note = $"{label}: {written}";
        if (orphaned > 0)
        {
            note += $", {orphaned} naming a permission the source itself does not have";
        }
        if (unmapped.Count > 0)
        {
            note += ", no modern counterpart for " + string.Join(", ", unmapped.Select(x => $"{x.Key} x{x.Value}"));
        }

        context.Logger.LogInformation("    {Label}: {Written} of {Total}", label, written, pending.Count);
        notes.Add(note);

        return written;
    }

    /// <summary>A gate whose legacy side names its target by id.</summary>
    private static async Task<int> CarryGateAsync(
        MigrationContext context,
        List<string> notes,
        string label,
        string legacySql,
        Dictionary<int, int> permissions,
        Func<int?, int?> resolveTarget,
        string insertSql,
        CancellationToken cancellationToken)
    {
        var pending = new List<(int PermissionId, int TargetId)>();
        var unresolved = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(legacySql, legacy) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1)
                    || !permissions.TryGetValue(reader.GetInt32(0), out var permissionId))
                {
                    unresolved++;
                    continue;
                }

                if (resolveTarget(reader.GetInt32(1)) is { } targetId)
                {
                    pending.Add((permissionId, targetId));
                }
                else
                {
                    unresolved++;
                }
            }
        }

        var written = 0;

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        {
            foreach (var (permissionId, targetId) in pending)
            {
                await using var command = new SqlCommand(insertSql, connection) { CommandTimeout = 1800 };
                command.Parameters.AddWithValue("@permissionId", permissionId);
                command.Parameters.AddWithValue("@target", targetId);

                written += await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        context.Logger.LogInformation("    {Label}: {Written}, {Unresolved} unresolvable", label, written, unresolved);
        notes.Add($"{label}: {written}" + (unresolved > 0 ? $", {unresolved} unresolvable" : string.Empty));

        return written;
    }

    private static async Task<Dictionary<string, int>> CodeMapAsync(
        MigrationContext context,
        string table,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand($"SELECT Code, Id FROM ensa.[{table}]", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            map[reader.GetString(0)] = reader.GetInt32(1);
        }

        return map;
    }

    private static async Task<Dictionary<int, int>> ReloadAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var fresh = new IdMap(context.Target.ModernConnectionString);
        return await fresh.LoadAsync("Yetki_T", cancellationToken);
    }

    private static async Task<int> ApplyPairsAsync(
        MigrationContext context,
        string template,
        List<(int First, int Second)> pending,
        CancellationToken cancellationToken)
    {
        if (pending.Count == 0)
        {
            return 0;
        }

        var written = 0;
        await using var connection = await context.OpenModernAsync(cancellationToken);

        foreach (var chunk in pending.Chunk(600))
        {
            var values = string.Join(",", chunk.Select((_, i) => $"(@a{i},@b{i})"));

            await using var command = new SqlCommand(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, template, values),
                connection) { CommandTimeout = 1800 };

            for (var i = 0; i < chunk.Length; i++)
            {
                command.Parameters.AddWithValue($"@a{i}", chunk[i].First);
                command.Parameters.AddWithValue($"@b{i}", chunk[i].Second);
            }

            written += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return written;
    }

    private static string? Text(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static string Fit(MigrationContext context, string table, string column, string? value, string? fallback)
        => context.Fitter.Fit(table, column, string.IsNullOrWhiteSpace(value) ? fallback : value) ?? string.Empty;
}
