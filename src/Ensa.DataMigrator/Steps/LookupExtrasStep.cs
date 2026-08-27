using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Communication;
using Ensa.Domain.Documents;
using Ensa.Domain.Health;
using Ensa.Domain.Lookups;
using Ensa.Domain.Menus;
using Ensa.Domain.Plans;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The reference lists the rest of the system is written in terms of: NACE codes, parameters,
/// icons, medication units, modules, activity periods and duties, number sequences and the sales
/// representatives.
/// <para>
/// Individually none of these is interesting; together they are the difference between a screen
/// that renders and one that shows a blank dropdown. They are carried in one step because they
/// share a shape — a handful of columns, no dependencies beyond the organization — and splitting
/// them would be nine steps that each do nothing.
/// </para>
/// </summary>
public sealed class LookupExtrasStep : IMigrationStep
{
    public int Order => 108;

    public string Name => "lookups";

    public string Description => "NACE codes, parameters, icons, modules, medication units, sales reps and the rest of the reference data";

    private const int BatchSize = 500;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await OccupationCodesAsync(context, cancellationToken),
            await IconLibrariesAsync(context, cancellationToken),
            await IconsAsync(context, cancellationToken),
            await ParametersAsync(context, cancellationToken),
            await SystemSettingsAsync(context, cancellationToken),
            await FormCategoriesAsync(context, cancellationToken),
            await MessageTemplateTypesAsync(context, cancellationToken),
            await NewsletterSubscribersAsync(context, cancellationToken),
            await ModulesAsync(context, cancellationToken),
            await ModuleParentsAsync(context, cancellationToken),
            await MedicationRoutesAsync(context, cancellationToken),
            await MedicationDoseUnitsAsync(context, cancellationToken),
            await MedicationFrequencyUnitsAsync(context, cancellationToken),
            await TreesAsync(context, cancellationToken),
            await TreeNodesAsync(context, cancellationToken),
            await SalesRepsAsync(context, cancellationToken),
            await SalesRepScreenFieldsAsync(context, cancellationToken),
            await EmailSettingsAsync(context, cancellationToken),
            await ActivityPeriodsAsync(context, cancellationToken),
            await ActivityDutiesAsync(context, cancellationToken),
            await NumberSequencesAsync(context, cancellationToken),
        };

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ codes and settings

    /// <summary>
    /// <c>MeslekKodu_T</c> to <see cref="OccupationCode"/> — the NACE list a workplace's hazard
    /// class is read from, which is why it is not optional reference data.
    /// </summary>
    private static Task<StepResult> OccupationCodesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<OccupationCode>(
            context, "MeslekKodu_T", "NACE codes",
            "SELECT MeslekKoduId, NACE_KODU, Tanim, TehlikeSinifi FROM MeslekKodu_T ORDER BY MeslekKoduId;",
            "MeslekKoduId",
            (reader, _) => new OccupationCode
            {
                NaceCode = Fit(context, "OccupationCode", "NaceCode", Text(reader, "NACE_KODU")) ?? string.Empty,
                Tag = Fit(context, "OccupationCode", "Tag", Text(reader, "Tanim")) ?? string.Empty,
                HazardClass = HazardClassOf(Text(reader, "TehlikeSinifi")),
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    private static Task<StepResult> IconLibrariesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<IconLibrary>(
            context, "IconLibrary_T", "icon libraries",
            "SELECT IconLibraryId, LibraryCode, LibraryName FROM IconLibrary_T ORDER BY IconLibraryId;",
            "IconLibraryId",
            (reader, _) => new IconLibrary
            {
                Code = Fit(context, "IconLibrary", "Code", Text(reader, "LibraryCode")) ?? string.Empty,
                Name = Fit(context, "IconLibrary", "Name", Text(reader, "LibraryName")) ?? string.Empty,
                IsActive = true,
                SortOrder = 0,
            },
            cancellationToken,
            reader => Text(reader, "LibraryCode") ?? string.Empty,
            library => library.Code);

    /// <summary>
    /// <c>Icon_T</c> to <see cref="Icon"/>, de-duplicated.
    /// <para>
    /// Three of the 810 rows list the same icon in the same library twice, and the destination
    /// declares that pair unique. An icon has nothing to distinguish one copy from another - no
    /// children, no dates, no different value - so the second copy is a duplicate in the ordinary
    /// sense and dropping it loses nothing. That is not true of every repeat in this migration,
    /// which is why it is decided per table rather than by a rule.
    /// </para>
    /// </summary>
    private static Task<StepResult> IconsAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<Icon>(
            context, "Icon_T", "icons",
            "SELECT IconId, IconLibraryCode, IconClass, ExtraProp FROM Icon_T ORDER BY IconId;",
            "IconId",
            (reader, _) => new Icon
            {
                LibraryCode = Fit(context, "Icon", "LibraryCode", Text(reader, "IconLibraryCode")) ?? string.Empty,
                IconCssClass = Fit(context, "Icon", "IconCssClass", Text(reader, "IconClass")) ?? string.Empty,
                ExtraFeature = Bit(reader, "ExtraProp"),
                SortOrder = 0,
            },
            cancellationToken,
            reader => (Text(reader, "IconLibraryCode") ?? string.Empty) + "|" + (Text(reader, "IconClass") ?? string.Empty),
            icon => icon.LibraryCode + "|" + icon.IconCssClass);

    private static async Task<StepResult> ParametersAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        // 40 of the 540 rows repeat a code within an organization, and the destination declares
        // (tenant, code) unique. A parameter is a setting rather than a history, so a second row
        // is a conflict and not a second reading; the lowest id wins, which is what the legacy
        // read path took as well - it asked for the first match.
        return await CopyAsync<Parameter>(
            context, "Parameter_T", "parameters",
            "SELECT ParameterId, ParameterCode, ParameterName, ParameterValue, Aktif, KurumId FROM Parameter_T ORDER BY ParameterId;",
            "ParameterId",
            (reader, _) => new Parameter
            {
                Code = Fit(context, "Parameter", "Code", Text(reader, "ParameterCode")) ?? string.Empty,
                Name = Fit(context, "Parameter", "Name", Text(reader, "ParameterName")) ?? string.Empty,
                Value = Fit(context, "Parameter", "Value", Text(reader, "ParameterValue")) ?? string.Empty,
                IsActive = Bit(reader, "Aktif"),
                CreationTime = DateTime.Now,
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken,
            reader => (organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenant) ? tenant : (int?)null)
                      + "|" + (Text(reader, "ParameterCode") ?? string.Empty),
            parameter => parameter.TenantId + "|" + parameter.Code);
    }

    /// <summary>
    /// <c>SabitDegiskenler_T</c> to <see cref="SystemSetting"/>.
    /// <para>
    /// The legacy table has no key column of its own — the variable name is the key — so the id
    /// map is keyed on the row's position, and a re-run recognises what is already there by name
    /// rather than by id.
    /// </para>
    /// </summary>
    private static async Task<StepResult> SystemSettingsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var taken = (await db.Set<SystemSetting>()
            .Select(s => s.SettingName).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var read = 0;
        var batch = new List<SystemSetting>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         "SELECT DegiskenAdi, Deger, DegiskenTipi, Sifreli, DegistirilebilirMi FROM SabitDegiskenler_T;",
                         connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var name = Fit(context, "SystemSetting", "SettingName", Text(reader, "DegiskenAdi"));
                if (name is null || !taken.Add(name))
                {
                    continue;
                }

                batch.Add(new SystemSetting
                {
                    SettingName = name,
                    Value = Fit(context, "SystemSetting", "Value", Text(reader, "Deger")) ?? string.Empty,
                    SettingType = Fit(context, "SystemSetting", "SettingType", Text(reader, "DegiskenTipi")) ?? string.Empty,
                    Encrypted = Bit(reader, "Sifreli"),
                    IsEditable = !reader.IsDBNull(reader.GetOrdinal("DegistirilebilirMi")) && Bit(reader, "DegistirilebilirMi"),
                });
            }
        }

        if (!context.DryRun && batch.Count > 0)
        {
            db.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new StepResult(read, batch.Count, read - batch.Count, $"system settings: {batch.Count} written");
    }

    private static async Task<StepResult> FormCategoriesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<FormCategory>(
            context, "FormKategori_T", "form categories",
            "SELECT KategoriId, KategoriAdi, KurumId, EklemeTarihi, GuncellemeTarihi FROM FormKategori_T ORDER BY KategoriId;",
            "KategoriId",
            (reader, _) => new FormCategory
            {
                CategoryName = Fit(context, "FormCategory", "CategoryName", Text(reader, "KategoriAdi")) ?? string.Empty,
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken,
            reader => Required(reader, "KurumId") + "|" + (Text(reader, "KategoriAdi") ?? string.Empty),
            category => category.TenantId + "|" + category.CategoryName);
    }

    private static Task<StepResult> MessageTemplateTypesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<MessageTemplateType>(
            context, "MessageType_T", "message template types",
            "SELECT MessageTypeId, MessageTypeCode, MessageType, CssClass FROM MessageType_T ORDER BY MessageTypeId;",
            "MessageTypeId",
            (reader, _) => new MessageTemplateType
            {
                Code = Fit(context, "MessageTemplateType", "Code", Text(reader, "MessageTypeCode")) ?? string.Empty,
                Name = Fit(context, "MessageTemplateType", "Name", Text(reader, "MessageType")) ?? string.Empty,
                CssClass = Fit(context, "MessageTemplateType", "CssClass", Text(reader, "CssClass")),
                CreationTime = DateTime.Now,
            },
            cancellationToken,
            reader => Text(reader, "MessageTypeCode") ?? string.Empty,
            type => type.Code);

    private static Task<StepResult> NewsletterSubscribersAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<NewsletterSubscriber>(
            context, "NewsletterEmail_T", "newsletter subscribers",
            "SELECT Id, Email, RegistrationDate FROM NewsletterEmail_T ORDER BY Id;",
            "Id",
            (reader, orphan) =>
            {
                var email = Fit(context, "NewsletterSubscriber", "Email", Text(reader, "Email"));
                if (email is null)
                {
                    orphan();
                    return null;
                }

                return new NewsletterSubscriber
                {
                    Email = email,
                    IsActive = true,
                    CreationTime = Date(reader, "RegistrationDate") ?? DateTime.Now,
                };
            },
            cancellationToken,
            reader => Text(reader, "Email") ?? string.Empty,
            subscriber => subscriber.Email);

    // ------------------------------------------------------------------ modules

    private static Task<StepResult> ModulesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<Module>(
            context, "Modul_T", "modules",
            "SELECT ModulId, ModulAdi, Aktif, EklemeTarihi, GuncellemeTarihi FROM Modul_T ORDER BY ModulId;",
            "ModulId",
            (reader, _) => new Module
            {
                Name = Fit(context, "Module", "Name", Text(reader, "ModulAdi")) ?? string.Empty,

                // Set by the pass below, once every module has an id.
                ParentModuleId = null,

                IsActive = Bit(reader, "Aktif"),
                SortOrder = 0,
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
            },
            cancellationToken);

    private static async Task<StepResult> ModuleParentsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        if (context.DryRun)
        {
            return new StepResult(0, 0, 0, null);
        }

        await using var db = context.CreateDbContext();

        var moduleMap = await context.IdMap.LoadAsync("Modul_T", cancellationToken);
        if (moduleMap.Count == 0)
        {
            return new StepResult(0, 0, 0, null);
        }

        var parents = new Dictionary<int, int>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         "SELECT ModulId, UstModulId FROM Modul_T WHERE UstModulId IS NOT NULL;", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (moduleMap.TryGetValue(Required(reader, "ModulId"), out var childId)
                    && moduleMap.TryGetValue(Required(reader, "UstModulId"), out var parentId))
                {
                    parents[childId] = parentId;
                }
            }
        }

        if (parents.Count == 0)
        {
            return new StepResult(0, 0, 0, null);
        }

        var ids = parents.Keys.ToList();
        var rows = await db.Set<Module>()
            .Where(m => ids.Contains(m.Id) && m.ParentModuleId == null)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.ParentModuleId = parents[row.Id];
        }

        await db.SaveChangesAsync(cancellationToken);

        return new StepResult(parents.Count, rows.Count, 0, $"module parents: {rows.Count} linked");
    }

    // ------------------------------------------------------------------ medication reference

    private static Task<StepResult> MedicationRoutesAsync(MigrationContext context, CancellationToken cancellationToken)
        => SkrsAsync<MedicationRoute>(
            context, "SKRS_IlacKullanimSekli_T", "SKRS_IlacKullanimSekliId", "medication routes", cancellationToken);

    private static Task<StepResult> MedicationDoseUnitsAsync(MigrationContext context, CancellationToken cancellationToken)
        => SkrsAsync<MedicationDoseUnit>(
            context, "SKRS_IlacKullanimDozBirimi_T", "SKRS_IlacKullanimDozBirimiId", "medication dose units", cancellationToken);

    private static Task<StepResult> MedicationFrequencyUnitsAsync(MigrationContext context, CancellationToken cancellationToken)
        => SkrsAsync<MedicationFrequencyUnit>(
            context, "SKRS_IlacKullanimPeriyoduBirimi_T", "SKRS_IlacKullanimPeriyoduBirimiId",
            "medication frequency units", cancellationToken);

    /// <summary>
    /// The three ministry medication reference lists, written with their legacy identities intact.
    /// <para>
    /// <b>Why identity insert.</b> <c>EReceteIlac_T</c> references these lists by row id rather
    /// than by code, and the prescription step carried those references across unchanged: 818,981
    /// medication lines already hold values from 1 to 26 that mean "row 1 of the dose unit list".
    /// Letting the destination allocate its own identities would leave every one of them pointing
    /// at whatever happened to land in that slot. So the rows are written by hand under
    /// <c>IDENTITY_INSERT</c>, and the ids are the same ones the prescriptions name.
    /// </para>
    /// <para>
    /// That also settles the dose unit list, which holds two overlapping ministry code sets under
    /// one type name — codes 1 to 5 appear twice, once as "MCG/KG/DAK, GRAM, MIKROGRAM…" and once
    /// as "Adet, Mililitre, Miligram…". Neither set can be dropped, because prescriptions
    /// reference rows from both.
    /// </para>
    /// </summary>
    private static async Task<StepResult> SkrsAsync<TEntity>(
        MigrationContext context,
        string legacyTable,
        string keyColumn,
        string label,
        CancellationToken cancellationToken)
        where TEntity : SkrsReferenceEntity, new()
    {
        var modernTable = "ensa." + typeof(TEntity).Name;

        var existing = new HashSet<int>();

        await using (var modern = await context.OpenModernAsync(cancellationToken))
        await using (var count = new SqlCommand($"SELECT Id FROM {modernTable};", modern))
        await using (var reader = await count.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                existing.Add(reader.GetInt32(0));
            }
        }

        var rows = new List<(int Id, string? CodeTypeName, string Name, int? Code, bool IsActive)>();
        var read = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         $"SELECT {keyColumn}, KodTipiAdi, Adi, Kodu, Aktif FROM {legacyTable} ORDER BY {keyColumn};",
                         legacy))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var id = Required(reader, keyColumn);
                if (existing.Contains(id))
                {
                    continue;
                }

                rows.Add((
                    id,
                    Fit(context, typeof(TEntity).Name, "CodeTypeName", Text(reader, "KodTipiAdi")),
                    Fit(context, typeof(TEntity).Name, "Name", Text(reader, "Adi")) ?? string.Empty,
                    Int(reader, "Kodu"),
                    Bit(reader, "Aktif")));
            }
        }

        if (rows.Count == 0 || context.DryRun)
        {
            return new StepResult(read, rows.Count, 0, $"{label}: {rows.Count} written (legacy ids kept)");
        }

        await using (var modern = await context.OpenModernAsync(cancellationToken))
        {
            // One connection for the whole insert: IDENTITY_INSERT is a per-session setting, so a
            // second connection would not see it and the insert would be refused.
            await using (var on = new SqlCommand($"SET IDENTITY_INSERT {modernTable} ON;", modern))
            {
                await on.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var row in rows)
            {
                await using var insert = new SqlCommand(
                    $"""
                     INSERT INTO {modernTable} (Id, CodeTypeName, Name, Code, IsActive, CreationTime)
                     VALUES (@id, @codeTypeName, @name, @code, @isActive, @creationTime);
                     """, modern);

                insert.Parameters.AddWithValue("@id", row.Id);
                insert.Parameters.AddWithValue("@codeTypeName", (object?)row.CodeTypeName ?? DBNull.Value);
                insert.Parameters.AddWithValue("@name", row.Name);
                insert.Parameters.AddWithValue("@code", (object?)row.Code ?? DBNull.Value);
                insert.Parameters.AddWithValue("@isActive", row.IsActive);
                insert.Parameters.AddWithValue("@creationTime", DateTime.Now);

                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var off = new SqlCommand($"SET IDENTITY_INSERT {modernTable} OFF;", modern))
            {
                await off.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        // The identity is the legacy id, so the map is the identity map.
        await context.IdMap.SaveAsync(
            legacyTable, rows.Select(r => (r.Id, r.Id)).ToList(), 'I', cancellationToken);

        return new StepResult(read, rows.Count, 0, $"{label}: {rows.Count} written (legacy ids kept)");
    }

    // ------------------------------------------------------------------ trees

    private static Task<StepResult> TreesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<Tree>(
            context, "Tree_T", "trees",
            "SELECT TreeId, TreeCode, TreeName, EklemeTarihi, GuncellemeTarihi FROM Tree_T ORDER BY TreeId;",
            "TreeId",
            (reader, _) => new Tree
            {
                TreeCode = Fit(context, "Tree", "TreeCode", Text(reader, "TreeCode")) ?? string.Empty,
                TreeName = Fit(context, "Tree", "TreeName", Text(reader, "TreeName")) ?? string.Empty,
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
            },
            cancellationToken,
            reader => Text(reader, "TreeCode") ?? string.Empty,
            tree => tree.TreeCode);

    /// <summary>
    /// <c>TreeItem_T</c> to <see cref="TreeNode"/>.
    /// <para>
    /// The legacy rows relate to each other by code rather than by id, and the destination keeps
    /// both — the codes because the penalties reference a node by code, and the ids because that
    /// is what a tree is walked by. The parent id is resolved from the codes in a second pass, so
    /// a node whose parent appears later in the table is still linked.
    /// </para>
    /// </summary>
    private static async Task<StepResult> TreeNodesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var trees = await db.Set<Tree>().Select(t => new { t.Id, t.TreeCode }).ToListAsync(cancellationToken);
        var treeByCode = trees.ToDictionary(t => t.TreeCode, t => t.Id, StringComparer.OrdinalIgnoreCase);

        var result = await CopyAsync<TreeNode>(
            context, "TreeItem_T", "tree nodes",
            """
            SELECT TreeItemId, TreeCode, TreeItemCode, ParentTreeItemCode, TreeItemName,
                   MainTreeItem, Aktif, IsDeleted, EklemeTarihi, GuncellemeTarihi
            FROM TreeItem_T ORDER BY TreeItemId;
            """,
            "TreeItemId",
            (reader, _) =>
            {
                var treeCode = Text(reader, "TreeCode") ?? string.Empty;

                return new TreeNode
                {
                    TreeCode = Fit(context, "TreeNode", "TreeCode", treeCode) ?? string.Empty,
                    TreeId = treeByCode.TryGetValue(treeCode, out var treeId) ? treeId : null,
                    TreeNodeCode = Fit(context, "TreeNode", "TreeNodeCode", Text(reader, "TreeItemCode")) ?? string.Empty,
                    ParentTreeNodeCode = Fit(context, "TreeNode", "ParentTreeNodeCode", Text(reader, "ParentTreeItemCode")),
                    ParentTreeNodeId = null,
                    TreeNodeName = Fit(context, "TreeNode", "TreeNodeName", Text(reader, "TreeItemName")) ?? string.Empty,
                    MainItem = Bit(reader, "MainTreeItem"),
                    IsActive = Bit(reader, "Aktif"),
                    IsDeleted = Bit(reader, "IsDeleted"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                };
            },
            cancellationToken);

        if (context.DryRun)
        {
            return result;
        }

        // Second pass: the parent code becomes a parent id.
        var nodes = await db.Set<TreeNode>()
            .Select(n => new { n.Id, n.TreeCode, n.TreeNodeCode, n.ParentTreeNodeCode, n.ParentTreeNodeId })
            .ToListAsync(cancellationToken);

        var byCode = nodes
            .GroupBy(n => (n.TreeCode, n.TreeNodeCode))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var linked = 0;

        foreach (var chunk in nodes.Where(n => n.ParentTreeNodeCode is not null && n.ParentTreeNodeId is null).Chunk(500))
        {
            var ids = chunk.Select(n => n.Id).ToList();
            var rows = await db.Set<TreeNode>().Where(n => ids.Contains(n.Id)).ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                if (row.ParentTreeNodeCode is { } parentCode
                    && byCode.TryGetValue((row.TreeCode, parentCode), out var parentId))
                {
                    row.ParentTreeNodeId = parentId;
                    linked++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        return result with { Note = result.Note + $"; {linked} parent link(s) resolved" };
    }

    // ------------------------------------------------------------------ sales

    /// <summary>
    /// <c>Temsilci_T</c> to <see cref="SalesRep"/>.
    /// <para>
    /// The legacy table carries a user name and a password of its own — the sales representatives
    /// signed in through a separate door. Neither is carried: the rebuilt system has one identity
    /// store, and copying a second set of credentials into a table that is not it would create
    /// accounts nothing authenticates and nobody can revoke. <c>UserId</c> is left unset for
    /// somebody to link deliberately.
    /// </para>
    /// </summary>
    private static Task<StepResult> SalesRepsAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<SalesRep>(
            context, "Temsilci_T", "sales representatives",
            "SELECT TemId, Adi, Soyadi, TemTuru FROM Temsilci_T ORDER BY TemId;",
            "TemId",
            (reader, _) => new SalesRep
            {
                Name = Fit(context, "SalesRep", "Name", Text(reader, "Adi")) ?? string.Empty,
                LastName = Fit(context, "SalesRep", "LastName", Text(reader, "Soyadi")) ?? string.Empty,
                UserId = null,
                SalesRepType = SalesRepTypeOf(Int(reader, "TemTuru")),
                IsActive = true,
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    private static Task<StepResult> SalesRepScreenFieldsAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<SalesRepScreenField>(
            context, "TemGosterAlan", "sales rep screen fields",
            "SELECT TemGosterId, AlanAdi, GAdi, Goster, TablodaGoster, PopuptaGoster, TemTuru, GosSirasi FROM TemGosterAlan ORDER BY TemGosterId;",
            "TemGosterId",
            (reader, _) => new SalesRepScreenField
            {
                FieldName = Fit(context, "SalesRepScreenField", "FieldName", Text(reader, "AlanAdi")) ?? string.Empty,
                DisplayedName = Fit(context, "SalesRepScreenField", "DisplayedName", Text(reader, "GAdi")) ?? string.Empty,
                Show = Bit(reader, "Goster"),
                InTableShow = Bit(reader, "TablodaGoster"),
                InPopupShow = Bit(reader, "PopuptaGoster"),
                ScreenType = ScreenTypeOf(Int(reader, "TemTuru")),
                SortOrder = Int(reader, "GosSirasi") ?? 0,
                CreationTime = DateTime.Now,
            },
            cancellationToken,
            reader => Int(reader, "TemTuru") + "|" + (Text(reader, "AlanAdi") ?? string.Empty),
            field => (int)field.ScreenType + "|" + field.FieldName);

    /// <summary>
    /// <c>AyarlarEmail_T</c> to <see cref="EmailSettings"/>.
    /// <para>
    /// <b>The password is not carried.</b> The legacy column holds an SMTP password in plain text;
    /// the destination column goes through the encrypting converter, so copying it would encrypt a
    /// credential that has been sitting readable in a database and call it protected. An
    /// organization's mail settings arrive with the server, the port and the account, and somebody
    /// re-enters the password once — which is also the moment it gets rotated.
    /// </para>
    /// </summary>
    private static async Task<StepResult> EmailSettingsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<EmailSettings>(
            context, "AyarlarEmail_T", "email settings",
            "SELECT Id, Email, POP3, SMTP, Port, KurumId FROM AyarlarEmail_T ORDER BY Id;",
            "Id",
            (reader, _) => new EmailSettings
            {
                Email = Fit(context, "EmailSettings", "Email", Text(reader, "Email")) ?? string.Empty,
                Password = string.Empty,
                Pop3Server = Fit(context, "EmailSettings", "Pop3Server", Text(reader, "POP3")) ?? string.Empty,
                SmtpServer = Fit(context, "EmailSettings", "SmtpServer", Text(reader, "SMTP")) ?? string.Empty,
                Port = int.TryParse(Text(reader, "Port"), out var port) ? port : 587,
                SslUse = true,

                // Inactive on purpose: the settings are incomplete until somebody supplies the
                // password, and a mail worker that picks them up meanwhile would fail on every send.
                IsActive = false,

                CreationTime = DateTime.Now,
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ activities and numbering

    private static async Task<StepResult> ActivityPeriodsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var activityMap = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);
        var periodMap = await context.IdMap.LoadAsync("Periyot_T", cancellationToken);

        return await CopyAsync<ActivityPeriod>(
            context, "AktivitePeriyot_T", "activity periods",
            "SELECT AktivitePeriyotId, PeriyotId, AktiviteId, EklemeTarihi FROM AktivitePeriyot_T ORDER BY AktivitePeriyotId;",
            "AktivitePeriyotId",
            (reader, orphan) =>
            {
                if (!activityMap.TryGetValue(Required(reader, "AktiviteId"), out var activityId)
                    || !periodMap.TryGetValue(Required(reader, "PeriyotId"), out var periodId))
                {
                    orphan();
                    return null;
                }

                return new ActivityPeriod
                {
                    ActivityId = activityId,
                    PeriodId = periodId,
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> ActivityDutiesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var activityMap = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);

        await using var db = context.CreateDbContext();
        var duties = await db.Set<Duty>().Select(d => new { d.Id, d.DutyCode }).ToListAsync(cancellationToken);
        var dutyByCode = duties
            .GroupBy(d => d.DutyCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        return await CopyAsync<ActivityDuty>(
            context, "AktiviteGorev_T", "activity duties",
            "SELECT AktiviteGorevId, AktiviteId, GorevKodu FROM AktiviteGorev_T ORDER BY AktiviteGorevId;",
            "AktiviteGorevId",
            (reader, orphan) =>
            {
                if (!activityMap.TryGetValue(Required(reader, "AktiviteId"), out var activityId))
                {
                    orphan();
                    return null;
                }

                var code = Text(reader, "GorevKodu");

                return new ActivityDuty
                {
                    ActivityId = activityId,
                    DutyCode = Fit(context, "ActivityDuty", "DutyCode", code),
                    DutyId = code is not null && dutyByCode.TryGetValue(code, out var dutyId) ? dutyId : null,
                    CreationTime = DateTime.Now,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>Numara_T</c> to <see cref="NumberSequence"/> — the last number handed out per company and
    /// document type, which is how the next invoice or report number is decided.
    /// </summary>
    private static async Task<StepResult> NumberSequencesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        // 140 of the 23,246 rows repeat a (company, type) pair, which the destination declares
        // unique. The row that survives is the one with the HIGHEST number, not the newest or the
        // first: this column is the last number handed out, and carrying a lower one would make
        // the system re-issue invoice and report numbers that are already in use. Ties go to the
        // later row.
        return await CopyAsync<NumberSequence>(
            context, "Numara_T", "number sequences",
            """
            SELECT NumaraId, FirmaId, Tur, Numara, Aktif, KurumId
            FROM (SELECT NumaraId, FirmaId, Tur, Numara, Aktif, KurumId,
                         ROW_NUMBER() OVER (PARTITION BY FirmaId, Tur
                                            ORDER BY Numara DESC, NumaraId DESC) AS rn
                  FROM Numara_T) AS ranked
            WHERE rn = 1
            ORDER BY NumaraId;
            """,
            "NumaraId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    orphan();
                    return null;
                }

                return new NumberSequence
                {
                    ScopeId = companyId,
                    Type = Fit(context, "NumberSequence", "Type", Text(reader, "Tur")) ?? string.Empty,
                    LatestNumber = Int(reader, "Numara") ?? 0,
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken,
            reader => Required(reader, "FirmaId") + "|" + (Text(reader, "Tur") ?? string.Empty),
            sequence => sequence.ScopeId + "|" + sequence.Type);
    }

    // ------------------------------------------------------------------ value mapping

    private static HazardClass HazardClassOf(string? hazardClass)
    {
        var folded = Fold(hazardClass);

        if (folded is null)
        {
            return HazardClass.Unspecified;
        }

        if (folded.Contains("COK TEHLIKELI", StringComparison.Ordinal))
        {
            return HazardClass.VeryHazardous;
        }

        if (folded.Contains("AZ TEHLIKELI", StringComparison.Ordinal))
        {
            return HazardClass.LowHazard;
        }

        return folded.Contains("TEHLIKELI", StringComparison.Ordinal)
            ? HazardClass.Hazardous
            : HazardClass.Unspecified;
    }

    private static SalesRepType SalesRepTypeOf(int? type)
        => type switch
        {
            1 => SalesRepType.FieldRepresentative,
            2 => SalesRepType.RegionOwner,
            3 => SalesRepType.Admin,
            _ => SalesRepType.Unspecified,
        };

    private static SalesRepScreenType ScreenTypeOf(int? type)
        => type switch
        {
            1 => SalesRepScreenType.ProspectCompany,
            2 => SalesRepScreenType.ContractedCompany,
            3 => SalesRepScreenType.Reference,
            _ => SalesRepScreenType.Unspecified,
        };

    private static string? Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var folded = value.Trim()
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ü', 'u').Replace('Ü', 'U')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ç', 'c').Replace('Ç', 'C')
            .ToUpperInvariant();

        return folded.Length == 0 ? null : folded;
    }

    // ------------------------------------------------------------------ the shared copy

    /// <summary>
    /// Reads one legacy table in id order, projects each row and writes it, recording the
    /// translation.
    /// </summary>
    /// <param name="uniqueKey">
    /// When given, the value the destination declares unique. A row whose key has already been
    /// seen is dropped and counted.
    /// <para>
    /// These are reference lists: an icon, a mail address or a code listed twice is a duplicate in
    /// the ordinary sense, with nothing to tell one copy from another and nothing pointing at
    /// either. That is emphatically not true of the repeats elsewhere in this migration — a
    /// repeated invoice number is a different invoice, a repeated training progress row is a
    /// retake — which is why the decision is made per table and never by a rule.
    /// </para>
    /// </param>
    private static async Task<StepResult> CopyAsync<TEntity>(
        MigrationContext context,
        string legacyTable,
        string label,
        string sql,
        string keyColumn,
        Func<SqlDataReader, Action, TEntity?> project,
        CancellationToken cancellationToken,
        Func<SqlDataReader, string>? uniqueKey = null,
        Func<TEntity, string>? existingKey = null)
        where TEntity : class
    {
        await using var db = context.CreateDbContext();

        var already = await context.IdMap.LoadAsync(legacyTable, cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var duplicates = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Seeded from the destination, not just from this run. A run that stopped halfway leaves
        // rows behind that the id map skips on the next pass, and a set that only knows what this
        // pass has produced would offer their duplicates again and collide on the same index that
        // stopped it the first time. These are reference tables of a few thousand rows, so reading
        // them back costs nothing worth saving.
        if (existingKey is not null)
        {
            foreach (var row in await db.Set<TEntity>().ToListAsync(cancellationToken))
            {
                seen.Add(existingKey(row));
            }

            db.ChangeTracker.Clear();
        }
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, TEntity Entity)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, keyColumn);
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (uniqueKey is not null && !seen.Add(uniqueKey(reader)))
                {
                    duplicates++;
                    continue;
                }

                var wasOrphaned = false;
                var entity = project(reader, () => wasOrphaned = true);

                if (entity is null || wasOrphaned)
                {
                    orphaned++;
                    continue;
                }

                batch.Add((legacyId, entity));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, legacyTable, batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, legacyTable, batch, pairs, cancellationToken);
        }

        var note = $"{label}: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED";
        }

        if (duplicates > 0)
        {
            note += $", {duplicates} DROPPED as a repeat of a key the destination holds unique";
        }

        return new StepResult(read, written, orphaned + duplicates, note);
    }

    // ------------------------------------------------------------------ helpers

    private static async Task<Dictionary<int, int>> LoadCompanyMapAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, int>(await context.IdMap.LoadAsync("Firma_T", cancellationToken));

        foreach (var (legacyId, modernId) in
                 await context.IdMap.LoadAsync("Firma_T:KurumSirket", cancellationToken))
        {
            map[legacyId] = modernId;
        }

        return map;
    }

    private static async Task<int> FlushAsync<TEntity>(
        DbContext db,
        MigrationContext context,
        string legacyTable,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        db.Set<TEntity>().AddRange(batch.Select(item => item.Entity));
        await db.SaveChangesAsync(cancellationToken);

        var chunkPairs = batch
            .Select(item => (item.LegacyId, (int)db.Entry(item.Entity).Property("Id").CurrentValue!))
            .ToList();

        await context.IdMap.SaveAsync(legacyTable, chunkPairs, 'I', cancellationToken);
        pairs.AddRange(chunkPairs);

        var count = batch.Count;
        batch.Clear();
        db.ChangeTracker.Clear();

        return count;
    }

    private static int DryRunFlush<TEntity>(
        MigrationContext context,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs)
    {
        pairs.AddRange(batch.Select(item => (item.LegacyId, context.NextDryRunId())));

        var count = batch.Count;
        batch.Clear();
        return count;
    }

    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

    private static string? Text(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index)?.ToString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int? Int(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToInt32(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static bool Bit(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetValue(index));
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
