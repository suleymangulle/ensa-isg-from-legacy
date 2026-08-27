using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Companies;
using Ensa.Domain.Documents;
using Ensa.Domain.Lookups;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// What each document is actually attached to, and the four small lists those attachments name.
/// <para>
/// <c>Dosya_T</c> knows only which company a file belongs to. Which record inside that company —
/// an employee's certificate, a department's inspection report, a device's calibration paper, a
/// specialist's assignment letter — is recorded by a separate link table per module, and each one
/// carries its own facts: an examination date, a validity date, who performed the examination.
/// They are the reason a document is worth anything, so they move together with it.
/// </para>
/// <para>
/// The 2.6 million employee document links are not here. They are a different kind of problem —
/// larger than any table but the visits — and are carried by
/// <see cref="EmployeeDocumentStep"/>.
/// </para>
/// </summary>
public sealed class DocumentLinkStep : IMigrationStep
{
    public int Order => 92;

    public string Name => "document-links";

    public string Description => "What each document is attached to: departments, devices, duties, specialists, standard documents";

    private const int BatchSize = 500;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await CertificatesAsync(context, cancellationToken),
            await DutiesAsync(context, cancellationToken),
            await StandardDocumentsAsync(context, cancellationToken),
            await EquipmentDocumentTypesAsync(context, cancellationToken),
            await EmployeeDutiesAsync(context, cancellationToken),
            await EmployeeDutyDocumentsAsync(context, cancellationToken),
            await SpecialistDocumentsAsync(context, cancellationToken),
            await DepartmentDocumentsAsync(context, cancellationToken),
            await EquipmentDocumentsAsync(context, cancellationToken),
            await CompanyStandardDocumentsAsync(context, cancellationToken),
        };

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ the four lists

    /// <summary><c>SertifikaListesi_T</c> to <see cref="Certificate"/>. Host-level: one list for all.</summary>
    private static Task<StepResult> CertificatesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<Certificate>(
            context, "SertifikaListesi_T", "certificates",
            """
            SELECT SertifikaId, SertifikaAdi, SertifikaKodu, EklemeTarihi, GuncellemeTarihi
            FROM SertifikaListesi_T ORDER BY SertifikaId;
            """,
            "SertifikaId",
            (reader, _) => new Certificate
            {
                CertificateName = Fit(context, "Certificate", "CertificateName", Text(reader, "SertifikaAdi")) ?? string.Empty,
                CertificateCode = Code(context, "Certificate", "CertificateCode",
                    Text(reader, "SertifikaKodu"), "sertifika", Required(reader, "SertifikaId")),
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
            },
            cancellationToken);

    /// <summary><c>Gorev_T</c> to <see cref="Duty"/> — the workplace roles an employee can be assigned.</summary>
    private static Task<StepResult> DutiesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<Duty>(
            context, "Gorev_T", "duties",
            "SELECT GorevId, GorevKodu, GorevAdi, GorevEtiketi, Aktif FROM Gorev_T ORDER BY GorevId;",
            "GorevId",
            (reader, _) => new Duty
            {
                DutyCode = Code(context, "Duty", "DutyCode",
                    Text(reader, "GorevKodu"), "gorev", Required(reader, "GorevId")),
                DutyName = Fit(context, "Duty", "DutyName", Text(reader, "GorevAdi")) ?? string.Empty,
                DutyLabel = Fit(context, "Duty", "DutyLabel", Text(reader, "GorevEtiketi")),
                IsActive = Bit(reader, "Aktif"),
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    /// <summary><c>SabitEvraklar_T</c> to <see cref="StandardDocument"/> — the paperwork every company owes.</summary>
    private static Task<StepResult> StandardDocumentsAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<StandardDocument>(
            context, "SabitEvraklar_T", "standard documents",
            """
            SELECT SabitEvrakId, SabitEvrakAdi, SabitEvraKodu, Aktif, EklemeTarihi, GuncellemeTarihi
            FROM SabitEvraklar_T ORDER BY SabitEvrakId;
            """,
            "SabitEvrakId",
            (reader, _) => new StandardDocument
            {
                StandardDocumentName = Fit(context, "StandardDocument", "StandardDocumentName", Text(reader, "SabitEvrakAdi")) ?? string.Empty,
                StandardDocumentCode = Code(context, "StandardDocument", "StandardDocumentCode",
                    Text(reader, "SabitEvraKodu"), "sabit-evrak", Required(reader, "SabitEvrakId")),
                IsActive = Bit(reader, "Aktif"),
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
            },
            cancellationToken);

    /// <summary><c>CihazEvrakListesi_T</c> to <see cref="EquipmentDocumentType"/>.</summary>
    private static async Task<StepResult> EquipmentDocumentTypesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<EquipmentDocumentType>(
            context, "CihazEvrakListesi_T", "equipment document types",
            """
            SELECT CihazEvrakId, EvrakAdi, KurumId, EklemeTarihi, GuncellemeTarihi
            FROM CihazEvrakListesi_T ORDER BY CihazEvrakId;
            """,
            "CihazEvrakId",
            (reader, _) => new EquipmentDocumentType
            {
                DocumentName = Fit(context, "EquipmentDocumentType", "DocumentName", Text(reader, "EvrakAdi")) ?? string.Empty,
                SortOrder = 0,
                IsActive = true,
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ the links

    /// <summary>
    /// <c>FirmaPersonelGorev_T</c> to <see cref="CompanyEmployeeDuty"/> — which employee holds
    /// which workplace role.
    /// </summary>
    private static async Task<StepResult> EmployeeDutiesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var dutyMap = await context.IdMap.LoadAsync("Gorev_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CompanyEmployeeDuty>(
            context, "FirmaPersonelGorev_T", "employee duties",
            """
            SELECT FirmaPersonelGorevId, FirmaPersonelId, GorevId, Aktif, KurumId,
                   EklemeTarihi, GuncellemeTarihi
            FROM FirmaPersonelGorev_T ORDER BY FirmaPersonelGorevId;
            """,
            "FirmaPersonelGorevId",
            (reader, orphan) =>
            {
                if (!employeeMap.TryGetValue(Required(reader, "FirmaPersonelId"), out var employeeId)
                    || !dutyMap.TryGetValue(Required(reader, "GorevId"), out var dutyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new CompanyEmployeeDuty
                {
                    CompanyEmployeeId = employeeId,
                    DutyId = dutyId,
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary><c>FirmaPersonelGorevDosya_T</c> to <see cref="CompanyEmployeeDutyDocument"/>.</summary>
    private static async Task<StepResult> EmployeeDutyDocumentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var dutyMap = await context.IdMap.LoadAsync("FirmaPersonelGorev_T", cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CompanyEmployeeDutyDocument>(
            context, "FirmaPersonelGorevDosya_T", "employee duty documents",
            """
            SELECT FirmaPersonelGorevDosyaId, FirmaPersonelGorevId, DosyaId, Aktif, EvrakTarihi,
                   KurumId, EklemeTarihi, GuncellemeTarihi
            FROM FirmaPersonelGorevDosya_T ORDER BY FirmaPersonelGorevDosyaId;
            """,
            "FirmaPersonelGorevDosyaId",
            (reader, orphan) =>
            {
                if (!dutyMap.TryGetValue(Required(reader, "FirmaPersonelGorevId"), out var dutyId)
                    || !documentMap.TryGetValue(Required(reader, "DosyaId"), out var documentId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new CompanyEmployeeDutyDocument
                {
                    CompanyEmployeeDutyId = dutyId,
                    DocumentId = documentId,
                    DocumentDate = Date(reader, "EvrakTarihi") ?? Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary><c>FirmaIlgilenenDosya_T</c> to <see cref="AssignedSpecialistDocument"/>.</summary>
    private static async Task<StepResult> SpecialistDocumentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var specialistMap = await context.IdMap.LoadAsync("FirmaIlgilenen_T", cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<AssignedSpecialistDocument>(
            context, "FirmaIlgilenenDosya_T", "specialist documents",
            """
            SELECT FirmaIlgilenenDosyaId, FirmaIlgilenenId, DosyaId, Aktif, EvrakTarihi,
                   KurumId, EklemeTarihi, GuncellemeTarihi
            FROM FirmaIlgilenenDosya_T ORDER BY FirmaIlgilenenDosyaId;
            """,
            "FirmaIlgilenenDosyaId",
            (reader, orphan) =>
            {
                if (!specialistMap.TryGetValue(Required(reader, "FirmaIlgilenenId"), out var specialistId)
                    || !documentMap.TryGetValue(Required(reader, "DosyaId"), out var documentId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new AssignedSpecialistDocument
                {
                    AssignedSpecialistId = specialistId,
                    DocumentId = documentId,
                    DocumentDate = Date(reader, "EvrakTarihi") ?? Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>IsyeriBolumEvrak_T</c> to <see cref="DepartmentDocument"/> — a department's inspection
    /// and measurement paperwork, with the dates that make it expire.
    /// </summary>
    private static async Task<StepResult> DepartmentDocumentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var departmentMap = await context.IdMap.LoadAsync("IsyeriBolum_T", cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var activityMap = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);
        var workPlanLineMap = await context.IdMap.LoadAsync("CalismaPlaniSatirlari_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<DepartmentDocument>(
            context, "IsyeriBolumEvrak_T", "department documents",
            """
            SELECT BolumEvrakId, BolumId, EvrakKodu, Aciklama, DosyaId, MuayeneYapan, MuayeneTarihi,
                   GecerlilikTarihi, AktiviteId, CalismaPlaniSatirId, IsDeleted, KurumId,
                   EklemeTarihi, GuncellemeTarihi
            FROM IsyeriBolumEvrak_T ORDER BY BolumEvrakId;
            """,
            "BolumEvrakId",
            (reader, orphan) =>
            {
                if (!departmentMap.TryGetValue(Required(reader, "BolumId"), out var departmentId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new DepartmentDocument
                {
                    WorkplaceDepartmentId = departmentId,
                    DocumentCode = Fit(context, "DepartmentDocument", "DocumentCode", Text(reader, "EvrakKodu")),
                    Description = Fit(context, "DepartmentDocument", "Description", Text(reader, "Aciklama")),
                    DocumentId = Lookup(documentMap, Int(reader, "DosyaId")),
                    ExaminationDate = Date(reader, "MuayeneTarihi"),
                    ValidityDate = Date(reader, "GecerlilikTarihi"),
                    ExaminationPerformedBy =
                        Fit(context, "DepartmentDocument", "ExaminationPerformedBy", Text(reader, "MuayeneYapan")),
                    ActivityId = Lookup(activityMap, Int(reader, "AktiviteId")),
                    WorkPlanLineId = Lookup(workPlanLineMap, Int(reader, "CalismaPlaniSatirId")),
                    IsDeleted = Bit(reader, "IsDeleted"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary><c>CihazEvrak_T</c> to <see cref="EquipmentDocument"/> — a device's calibration and inspection papers.</summary>
    private static async Task<StepResult> EquipmentDocumentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var equipmentMap = await context.IdMap.LoadAsync("Cihaz_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var activityMap = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);
        var workPlanLineMap = await context.IdMap.LoadAsync("CalismaPlaniSatirlari_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<EquipmentDocument>(
            context, "CihazEvrak_T", "equipment documents",
            """
            SELECT CihazEvrakId, CihazId, FirmaId, DosyaId, Aciklama, GecerlilikTarihi,
                   MuayeneYapan, MuayeneTarihi, AktiviteId, CalismaPlaniSatirId, KurumId,
                   EklemeTarihi, GuncellemeTarihi
            FROM CihazEvrak_T ORDER BY CihazEvrakId;
            """,
            "CihazEvrakId",
            (reader, orphan) =>
            {
                // The document is required here, not optional: an equipment document row with no
                // file is a claim that paperwork exists, with nothing behind it.
                if (!equipmentMap.TryGetValue(Required(reader, "CihazId"), out var equipmentId)
                    || !companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !documentMap.TryGetValue(Required(reader, "DosyaId"), out var documentId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new EquipmentDocument
                {
                    EquipmentId = equipmentId,
                    CompanyId = companyId,
                    DocumentId = documentId,

                    // CihazEvrak_T names no type. CihazEvrakListesi_T is a list the legacy schema
                    // never joins to it, so the link is left unset rather than invented.
                    EquipmentDocumentTypeId = null,

                    Description = Fit(context, "EquipmentDocument", "Description", Text(reader, "Aciklama")),
                    ExaminationDate = Date(reader, "MuayeneTarihi"),
                    ValidityDate = Date(reader, "GecerlilikTarihi"),
                    ExaminationPerformedBy =
                        Fit(context, "EquipmentDocument", "ExaminationPerformedBy", Text(reader, "MuayeneYapan")),
                    ActivityId = Lookup(activityMap, Int(reader, "AktiviteId")),
                    WorkPlanLineId = Lookup(workPlanLineMap, Int(reader, "CalismaPlaniSatirId")),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>SabitEvraklarFirma_T</c> to <see cref="CompanyStandardDocument"/> — which of the standard
    /// documents each company has produced, and which it still owes.
    /// </summary>
    private static async Task<StepResult> CompanyStandardDocumentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var standardMap = await context.IdMap.LoadAsync("SabitEvraklar_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CompanyStandardDocument>(
            context, "SabitEvraklarFirma_T", "company standard documents",
            """
            SELECT FirmaSabitEvrakId, SabitEvrakId, FirmaId, DosyaId, Durum, OnayDurumu,
                   EvrakTarihi, KurumId, EklemeTarihi, GuncellemeTarihi
            FROM SabitEvraklarFirma_T ORDER BY FirmaSabitEvrakId;
            """,
            "FirmaSabitEvrakId",
            (reader, orphan) =>
            {
                if (!standardMap.TryGetValue(Required(reader, "SabitEvrakId"), out var standardId)
                    || !companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new CompanyStandardDocument
                {
                    StandardDocumentId = standardId,
                    CompanyId = companyId,

                    // The file is genuinely optional: 235,319 of these rows are a company's
                    // outstanding paperwork, recorded before anything was uploaded.
                    DocumentId = Lookup(documentMap, Int(reader, "DosyaId")),

                    ApprovalStatus = ApprovalOf(Int(reader, "Durum")),
                    DocumentDate = Date(reader, "EvrakTarihi"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>SabitEvraklarFirma_T.Durum</c>, which holds <c>-1</c>, <c>0</c> or nothing at all.
    /// <para>
    /// <c>OnayDurumu</c> — the column actually named "approval status" — is null in all 236,345
    /// rows, so it carries nothing. <c>Durum</c> is set on 1,026 of them and its <c>-1</c> is the
    /// legacy convention for a rejection. Everything else, the 235,319 rows included, is a draft:
    /// the company owes the document and nothing has been decided about it.
    /// </para>
    /// </summary>
    private static ApprovalStatus ApprovalOf(int? status)
        => status == -1 ? ApprovalStatus.Rejected : ApprovalStatus.Draft;

    // ------------------------------------------------------------------ the shared copy

    /// <summary>
    /// Reads one legacy table in id order, projects each row and writes it through the model,
    /// recording the translation. Rows the projection returns <c>null</c> for are counted as
    /// orphans and not written.
    /// </summary>
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

    /// <summary>
    /// A code for a lookup whose code column carries a unique index.
    /// <para>
    /// 51 of the 54 standard documents have no code at all, and an empty string is a value like
    /// any other to a unique index: the second one collides and the migration stops. Falling back
    /// to the legacy id gives every row a code that is unique, stable across re-runs, and visibly
    /// generated rather than passed off as something the legacy system recorded.
    /// </para>
    /// </summary>
    private static string Code(
        MigrationContext context,
        string table,
        string column,
        string? value,
        string prefix,
        int legacyId)
        => Fit(context, table, column, value) ?? $"{prefix}-{legacyId}";

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
