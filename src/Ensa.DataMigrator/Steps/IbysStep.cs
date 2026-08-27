using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Ibys;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// IBYS — the ministry's notification system: its reference lists, the submissions made to it,
/// and the electronic signature licence they are signed with.
/// <para>
/// Every training and every medical examination an OSGB performs has to be notified to the
/// Ministry of Labour, and the notification carries codes from the ministry's own lists: the
/// ISCO-08 occupation, the working environment, the work arrangement, the equipment. Those lists
/// are the reason a form can be submitted at all, and none of them had been carried.
/// </para>
/// <para>
/// <b>The occupation list is 2,120,971 rows and 7,339 occupations.</b> The legacy table holds each
/// code 289 times over — the ministry's list re-imported once per import and never de-duplicated.
/// The destination declares the code unique, correctly, so the distinct pairs are what moves. This
/// is the one place in this migration where reading fewer rows than the source has is the faithful
/// answer rather than a loss.
/// </para>
/// </summary>
public sealed class IbysStep : IMigrationStep
{
    public int Order => 102;

    public string Name => "ibys";

    public string Description => "IBYS reference lists, submissions and the signature licence";

    private const int BatchSize = 500;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await OccupationCodesAsync(context, cancellationToken),
            await RootReferencesAsync(context, cancellationToken),
            await ChildReferencesAsync(context, cancellationToken),
            await EnvironmentTypesAsync(context, cancellationToken),
            await EnvironmentsAsync(context, cancellationToken),
            await ArrangementsAsync(context, cancellationToken),
            await EquipmentCategoriesAsync(context, cancellationToken),
            await EquipmentAsync(context, cancellationToken),
            await LicencesAsync(context, cancellationToken),
            await QueriesAsync(context, cancellationToken),
            await ServedWorkplacesAsync(context, cancellationToken),
            await LinkFormsToQueriesAsync(context, cancellationToken),
        };

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ the reference lists

    /// <summary>
    /// The ISCO-08 occupation list, de-duplicated on the way in.
    /// <para>
    /// Written straight rather than through the shared copy: 7,339 rows keyed by a code nobody
    /// looks up by legacy id, and the legacy ids are 2.1 million of them anyway.
    /// </para>
    /// </summary>
    private static async Task<StepResult> OccupationCodesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var taken = (await db.Set<IbysIsco08OccupationCode>()
            .Select(o => o.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

        var read = 0;
        var written = 0;
        var batch = new List<IbysIsco08OccupationCode>();

        // DISTINCT in the database rather than in memory: 2.1 million rows across the wire to
        // discard 99.7% of them is the work this query exists to avoid.
        const string sql = """
            SELECT DISTINCT Kod, Ad FROM IBYSIsco08MeslekKodlari_T
            WHERE Kod IS NOT NULL AND LTRIM(RTRIM(Kod)) <> '' ORDER BY Kod;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var code = Fit(context, "IbysIsco08OccupationCode", "Code", Text(reader, "Kod"));
                if (code is null || !taken.Add(code))
                {
                    continue;
                }

                batch.Add(new IbysIsco08OccupationCode
                {
                    Code = code,
                    Name = Fit(context, "IbysIsco08OccupationCode", "Name", Text(reader, "Ad")) ?? string.Empty,
                    IsActive = true,
                    CreationTime = DateTime.Now,
                });

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    db.AddRange(batch);
                    await db.SaveChangesAsync(cancellationToken);
                    written += batch.Count;
                    batch.Clear();
                    db.ChangeTracker.Clear();
                }
            }
        }

        if (batch.Count > 0)
        {
            if (!context.DryRun)
            {
                db.AddRange(batch);
                await db.SaveChangesAsync(cancellationToken);
            }

            written += batch.Count;
        }

        return new StepResult(
            read, written, 0,
            $"occupation codes: {written} written from {read} distinct pair(s) in 2,120,971 legacy rows");
    }

    private static Task<StepResult> RootReferencesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<IbysRootReferenceValue>(
            context, "IBYSUstReferansDegerler_T", "root references",
            "SELECT Id, Kod, ReferansAdi, AktifMi FROM IBYSUstReferansDegerler_T ORDER BY Id;",
            "Id",
            (reader, _) => new IbysRootReferenceValue
            {
                Code = Fit(context, "IbysRootReferenceValue", "Code", Text(reader, "Kod")) ?? string.Empty,
                ReferenceName = Fit(context, "IbysRootReferenceValue", "ReferenceName", Text(reader, "ReferansAdi")) ?? string.Empty,
                IsActive = Bit(reader, "AktifMi"),
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    private static async Task<StepResult> ChildReferencesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        // The legacy child rows name their parent by code; the destination also keeps the code and
        // adds a real reference, so the parent is looked up by what the child already says.
        var roots = await db.Set<IbysRootReferenceValue>()
            .Select(r => new { r.Id, r.Code })
            .ToListAsync(cancellationToken);

        var byCode = roots.ToDictionary(r => r.Code, r => r.Id, StringComparer.OrdinalIgnoreCase);

        return await CopyAsync<IbysChildReferenceValue>(
            context, "IBYSBagliReferansDegerler_T", "child references",
            "SELECT ID, Kod, ReferansAdi, UstReferansKodu, AktifMi FROM IBYSBagliReferansDegerler_T ORDER BY ID;",
            "ID",
            (reader, _) =>
            {
                var parentCode = Text(reader, "UstReferansKodu") ?? string.Empty;

                return new IbysChildReferenceValue
                {
                    Code = Fit(context, "IbysChildReferenceValue", "Code", Text(reader, "Kod")) ?? string.Empty,
                    ReferenceName = Fit(context, "IbysChildReferenceValue", "ReferenceName", Text(reader, "ReferansAdi")) ?? string.Empty,
                    ParentReferenceCode = Fit(context, "IbysChildReferenceValue", "ParentReferenceCode", parentCode) ?? string.Empty,
                    IbysRootReferenceValueId = byCode.TryGetValue(parentCode, out var rootId) ? rootId : null,
                    IsActive = Bit(reader, "AktifMi"),
                    CreationTime = DateTime.Now,
                };
            },
            cancellationToken);
    }

    private static Task<StepResult> EnvironmentTypesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<IbysWorkEnvironmentType>(
            context, "IBYSCalismaOrtamTurleri_T", "work environment types",
            "SELECT Id, TurKodu, TurAdi, Aktif FROM IBYSCalismaOrtamTurleri_T ORDER BY Id;",
            "Id",
            (reader, _) => new IbysWorkEnvironmentType
            {
                TypeCode = Required(reader, "TurKodu"),
                TypeName = Fit(context, "IbysWorkEnvironmentType", "TypeName", Text(reader, "TurAdi")) ?? string.Empty,
                IsActive = Bit(reader, "Aktif"),
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    private static async Task<StepResult> EnvironmentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var types = await db.Set<IbysWorkEnvironmentType>()
            .Select(t => new { t.Id, t.TypeCode })
            .ToListAsync(cancellationToken);

        var byTypeCode = types
            .GroupBy(t => t.TypeCode)
            .ToDictionary(g => g.Key, g => g.First().Id);

        return await CopyAsync<IbysWorkEnvironment>(
            context, "IBYSCalismaOrtamlari_T", "work environments",
            "SELECT Id, OrtamKodu, Ortam, TurKodu, Aktif FROM IBYSCalismaOrtamlari_T ORDER BY Id;",
            "Id",
            (reader, _) =>
            {
                var typeCode = Required(reader, "TurKodu");

                return new IbysWorkEnvironment
                {
                    EnvironmentCode = Required(reader, "OrtamKodu"),
                    Environment = Fit(context, "IbysWorkEnvironment", "Environment", Text(reader, "Ortam")) ?? string.Empty,
                    TypeCode = typeCode,
                    IbysWorkEnvironmentTypeId = byTypeCode.TryGetValue(typeCode, out var typeId) ? typeId : null,
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = DateTime.Now,
                };
            },
            cancellationToken);
    }

    private static Task<StepResult> ArrangementsAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<IbysWorkArrangement>(
            context, "IBYSCalismaSekilleri_T", "work arrangements",
            "SELECT Id, Kod, Ad, Tur, Aciklama FROM IBYSCalismaSekilleri_T ORDER BY Id;",
            "Id",
            (reader, _) => new IbysWorkArrangement
            {
                Code = Required(reader, "Kod"),
                Name = Fit(context, "IbysWorkArrangement", "Name", Text(reader, "Ad")) ?? string.Empty,
                Type = Required(reader, "Tur"),
                Description = Fit(context, "IbysWorkArrangement", "Description", Text(reader, "Aciklama")),
                IsActive = true,
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    private static Task<StepResult> EquipmentCategoriesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<IbysEquipmentTopCategory>(
            context, "IBYSIsEkipmanUstKategorileri_T", "equipment categories",
            "SELECT Id, UstKategoriAdi FROM IBYSIsEkipmanUstKategorileri_T ORDER BY Id;",
            "Id",
            (reader, _) => new IbysEquipmentTopCategory
            {
                ParentCategoryName =
                    Fit(context, "IbysEquipmentTopCategory", "ParentCategoryName", Text(reader, "UstKategoriAdi")) ?? string.Empty,
                IsActive = true,
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    private static async Task<StepResult> EquipmentAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var categoryMap = await context.IdMap.LoadAsync("IBYSIsEkipmanUstKategorileri_T", cancellationToken);

        return await CopyAsync<IbysWorkEquipment>(
            context, "IBYSIsEkipmanlari_T", "work equipment",
            "SELECT Id, Kod, Ad, UstKategoriId FROM IBYSIsEkipmanlari_T ORDER BY Id;",
            "Id",
            (reader, orphan) =>
            {
                if (!categoryMap.TryGetValue(Required(reader, "UstKategoriId"), out var categoryId))
                {
                    orphan();
                    return null;
                }

                return new IbysWorkEquipment
                {
                    Code = Required(reader, "Kod"),
                    Name = Fit(context, "IbysWorkEquipment", "Name", Text(reader, "Ad")) ?? string.Empty,
                    ParentCategoryId = categoryId,
                    IsActive = true,
                    CreationTime = DateTime.Now,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ submissions

    private static Task<StepResult> LicencesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<ESignatureLicense>(
            context, "ArksignerLisans_T", "signature licences",
            "SELECT Id, Lisans, Aktif, GecerlilikTarihi, EklenmeTarihi FROM ArksignerLisans_T ORDER BY Id;",
            "Id",
            (reader, _) => new ESignatureLicense
            {
                License = Fit(context, "ESignatureLicense", "License", Text(reader, "Lisans")) ?? string.Empty,
                ValidityDate = Date(reader, "GecerlilikTarihi") ?? DateTime.Now,
                IsActive = Bit(reader, "Aktif"),
                CreationTime = Date(reader, "EklenmeTarihi") ?? DateTime.Now,
            },
            cancellationToken);

    /// <summary>
    /// <c>IBYSSorguNo_T</c> to <see cref="IbysQuery"/> — every notification sent to the ministry
    /// and what came back.
    /// <para>
    /// The legacy table carries no organization, and the destination is tenant-scoped. The tenant
    /// is taken from the company the notification is about; a notification that names neither a
    /// company nor an employee is written host-level, which is where a submission nobody can
    /// attribute belongs.
    /// </para>
    /// </summary>
    private static async Task<StepResult> QueriesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var companyTenants = await LoadTenantsAsync(context, "ensa.Company", cancellationToken);

        return await CopyAsync<IbysQuery>(
            context, "IBYSSorguNo_T", "submissions",
            """
            SELECT SorgunoId, SorguNo, SorguTur, DurumKodu, IBYSDurum, IbysMesaji, GonderimTarihi,
                   GrupId, IbysVersion, ZamanDamgasi, FirmaId, FirmaPersonelId, XmlVeri, ImzaliVeri
            FROM IBYSSorguNo_T ORDER BY SorgunoId;
            """,
            "SorgunoId",
            (reader, _) =>
            {
                var companyId = Lookup(companyMap, Int(reader, "FirmaId"));

                return new IbysQuery
                {
                    QueryNo = Fit(context, "IbysQuery", "QueryNo", Text(reader, "SorguNo")),
                    QueryType = QueryTypeOf(Text(reader, "SorguTur")),
                    Status = StatusOf(Int(reader, "IBYSDurum")),
                    StatusCode = Int(reader, "DurumKodu") ?? 0,
                    IbysMessage = Fit(context, "IbysQuery", "IbysMessage", Text(reader, "IbysMesaji")),
                    SubmissionDate = Date(reader, "GonderimTarihi") ?? DateTime.Now,
                    GroupId = Fit(context, "IbysQuery", "GroupId", Text(reader, "GrupId")),
                    IbysVersion = Fit(context, "IbysQuery", "IbysVersion", Text(reader, "IbysVersion")),
                    TimeStamp = Fit(context, "IbysQuery", "TimeStamp", Text(reader, "ZamanDamgasi")),
                    CompanyId = companyId,
                    CompanyEmployeeId = Lookup(employeeMap, Int(reader, "FirmaPersonelId")),

                    // The submitted XML and its signature are the evidence that the notification
                    // was made and what it said. Both go through the destination's encryption.
                    XmlData = Text(reader, "XmlVeri"),
                    SignedData = Text(reader, "ImzaliVeri"),

                    CreationTime = Date(reader, "GonderimTarihi") ?? DateTime.Now,
                    TenantId = companyId is int id && companyTenants.TryGetValue(id, out var tenantId)
                        ? tenantId
                        : null,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> ServedWorkplacesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<IbysServedWorkplace>(
            context, "IBYSHizmetVerilenIsyeri_T", "served workplaces",
            """
            SELECT Id, FirmaId, OnaylayanKullanici, XmlVeri, ImzaliVeri, IBYSBildirimNo, Aktif,
                   KurumId, HizmetBaslangicTarihi, HizmetBitisTarihi, EklenmeTarihi, GuncellenmeTarihi
            FROM IBYSHizmetVerilenIsyeri_T ORDER BY Id;
            """,
            "Id",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !userMap.TryGetValue(Required(reader, "OnaylayanKullanici"), out var approverId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new IbysServedWorkplace
                {
                    CompanyId = companyId,
                    ApproverUserId = approverId,
                    ServiceStartDate = Date(reader, "HizmetBaslangicTarihi") ?? DateTime.Now,
                    ServiceEndDate = Date(reader, "HizmetBitisTarihi"),
                    IbysNotificationNo =
                        Fit(context, "IbysServedWorkplace", "IbysNotificationNo", Text(reader, "IBYSBildirimNo")),
                    XmlData = Text(reader, "XmlVeri"),
                    SignedData = Text(reader, "ImzaliVeri"),
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = Date(reader, "EklenmeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellenmeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// Points each medical examination form at the submission that carried it.
    /// <para>
    /// A second pass, because the forms were written before the submissions existed. 4,526 of the
    /// 9,865 forms name one, and until now that column was null on every row — the form said it
    /// had been notified and could not say by which notification.
    /// </para>
    /// </summary>
    private static async Task<StepResult> LinkFormsToQueriesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        if (context.DryRun)
        {
            return new StepResult(0, 0, 0, null);
        }

        await using var db = context.CreateDbContext();

        var formMap = await context.IdMap.LoadAsync("PeriyodikMuayeneFormu_T", cancellationToken);
        var queryMap = await context.IdMap.LoadAsync("IBYSSorguNo_T", cancellationToken);

        if (formMap.Count == 0 || queryMap.Count == 0)
        {
            return new StepResult(0, 0, 0, null);
        }

        var links = new Dictionary<int, int>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         "SELECT PeriyodikMuayeneFormuId, IBYSSorguId FROM PeriyodikMuayeneFormu_T "
                         + "WHERE IBYSSorguId IS NOT NULL;", connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (formMap.TryGetValue(Required(reader, "PeriyodikMuayeneFormuId"), out var formId)
                    && queryMap.TryGetValue(Required(reader, "IBYSSorguId"), out var queryId))
                {
                    links[formId] = queryId;
                }
            }
        }

        var written = 0;

        foreach (var chunk in links.Chunk(500))
        {
            var ids = chunk.Select(pair => pair.Key).ToList();

            var forms = await db.Set<Ensa.Domain.Health.MedicalExaminationForm>()
                .Where(f => ids.Contains(f.Id) && f.IbysQueryId == null)
                .ToListAsync(cancellationToken);

            foreach (var form in forms)
            {
                form.IbysQueryId = links[form.Id];
            }

            written += forms.Count;
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        return new StepResult(links.Count, written, 0, $"form to submission links: {written} set");
    }

    // ------------------------------------------------------------------ value mapping

    /// <summary><c>SorguTur</c>: "egitim" or "muayene".</summary>
    private static IbysQueryType QueryTypeOf(string? type)
        => Fold(type) switch
        {
            "EGITIM" => IbysQueryType.Training,
            "MUAYENE" => IbysQueryType.HealthReport,
            "ISYERI" => IbysQueryType.ServiceProvidedWorkplace,
            _ => IbysQueryType.Unspecified,
        };

    /// <summary>
    /// <c>IBYSDurum</c>: 1 accepted, -1 rejected, 0 prepared. The same three values the employee
    /// documents and the examination forms use, read the same way.
    /// </summary>
    private static IbysSubmissionStatus StatusOf(int? status)
        => status switch
        {
            1 => IbysSubmissionStatus.Approved,
            -1 => IbysSubmissionStatus.Failed,
            0 => IbysSubmissionStatus.Prepared,
            _ => IbysSubmissionStatus.NotSent,
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

    private static async Task<StepResult> CopyAsync<TEntity>(
        MigrationContext context,
        string legacyTable,
        string label,
        string sql,
        string keyColumn,
        Func<SqlDataReader, Action, TEntity?> project,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        await using var db = context.CreateDbContext();

        var already = await context.IdMap.LoadAsync(legacyTable, cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
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
            note += $", {orphaned} SKIPPED (a referenced record is missing)";
        }

        return new StepResult(read, written, orphaned, note);
    }

    // ------------------------------------------------------------------ helpers

    private static async Task<Dictionary<int, int?>> LoadTenantsAsync(
        MigrationContext context,
        string modernTable,
        CancellationToken cancellationToken)
    {
        var tenants = new Dictionary<int, int?>();

        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand($"SELECT Id, TenantId FROM {modernTable};", connection)
        {
            CommandTimeout = 600,
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tenants[reader.GetInt32(0)] = reader.IsDBNull(1) ? null : reader.GetInt32(1);
        }

        return tenants;
    }

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

    private static int? Lookup(Dictionary<int, int> map, int? legacyId)
        => legacyId is int id && map.TryGetValue(id, out var modernId) ? modernId : null;

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
