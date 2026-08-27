using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Documents;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The four operational records the earlier steps left behind: field observations, corrective
/// actions, incidents and emergency action plans.
/// <para>
/// These are the modules an inspection actually asks for. A field observation records what was
/// found wrong on the floor; a corrective action records what was done about it — 3,296 of the
/// 3,365 name the observation line they came from, so the two only mean anything together. An
/// incident record is the workplace's accident history. An emergency action plan is a legal
/// document with a validity date.
/// </para>
/// <para>
/// <b>Two of these tables hold their files inline.</b> <c>SahaGozlemRaporuSatirlari_T.Dosya</c> is
/// 749 MB across 2,909 rows and <c>AcilDurumEylemPlani_T.TahliyePlani</c> is 63 MB across 87. The
/// rebuilt schema has one document store and every module points at it, so those become
/// <see cref="Document"/> rows like any other file: metadata written here, payload placed by
/// <c>--export-documents</c>, which knows about both columns.
/// </para>
/// </summary>
public sealed class OperationsExtraStep : IMigrationStep
{
    public int Order => 96;

    public string Name => "operations-extra";

    public string Description => "Field observations, corrective actions, incidents and emergency action plans";

    private const int BatchSize = 500;

    /// <summary>
    /// The id map keys under which an inline binary's <see cref="Document"/> row is recorded, and
    /// the keys <c>--export-documents</c> looks the payloads up by. Changing one means changing
    /// the other.
    /// </summary>
    public const string FieldObservationBlobs = "SahaGozlemRaporuSatirlari_T:Dosya";

    public const string EvacuationPlanBlobs = "AcilDurumEylemPlani_T:TahliyePlani";

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await FieldObservationReportsAsync(context, cancellationToken),
            await FieldObservationBlobsAsync(context, cancellationToken),
            await FieldObservationLinesAsync(context, cancellationToken),
            await CorrectiveActionsAsync(context, cancellationToken),
            await IncidentsAsync(context, cancellationToken),
            await IncidentPeopleAsync(context, cancellationToken),
            await EvacuationPlanBlobsAsync(context, cancellationToken),
            await EmergencyPlansAsync(context, cancellationToken),
            await EmergencyPlanSectionsAsync(context, cancellationToken),
            await EmergencyTeamMembersAsync(context, cancellationToken),
        };

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ field observations

    private static async Task<StepResult> FieldObservationReportsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var departmentMap = await context.IdMap.LoadAsync("IsyeriBolum_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<FieldObservationReport>(
            context, "SahaGozlemRaporu_T", "field observation reports",
            """
            SELECT SahaGozlemRaporuId, FirmaId, BolumId, Tarih, KurumId, EklemeTarihi, GuncellemeTarihi
            FROM SahaGozlemRaporu_T ORDER BY SahaGozlemRaporuId;
            """,
            "SahaGozlemRaporuId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new FieldObservationReport
                {
                    CompanyId = companyId,
                    DepartmentId = Lookup(departmentMap, Int(reader, "BolumId")),
                    Date = Date(reader, "Tarih") ?? Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// The photographs attached to observation lines become <see cref="Document"/> rows.
    /// <para>
    /// Written before the lines, because the line carries the document's id and the document has
    /// to exist to have one. The payload is not read: <c>DATALENGTH</c> gives the size and
    /// <c>--export-documents</c> places the bytes.
    /// </para>
    /// </summary>
    private static async Task<StepResult> FieldObservationBlobsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var reportMap = await context.IdMap.LoadAsync("SahaGozlemRaporu_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<Document>(
            context, FieldObservationBlobs, "field observation photographs",
            """
            SELECT s.SahaGozlemSatiriId, s.SahaGozlemRaporuId, s.DosyaAdi, s.DosyaTuru, s.KurumId,
                   CAST(DATALENGTH(s.Dosya) AS bigint) AS Boyut
            FROM SahaGozlemRaporuSatirlari_T AS s
            WHERE DATALENGTH(s.Dosya) > 0
            ORDER BY s.SahaGozlemSatiriId;
            """,
            "SahaGozlemSatiriId",
            (reader, orphan) =>
            {
                if (!organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                var legacyId = Required(reader, "SahaGozlemSatiriId");
                var name = Text(reader, "DosyaAdi") ?? $"saha-gozlem-{legacyId}";
                var storageName = DocumentStep.DeriveStorageName(FieldObservationBlobs, legacyId);

                return new Document
                {
                    DocumentName = Fit(context, "Document", "DocumentName", name) ?? $"saha-gozlem-{legacyId}",
                    StorageName = storageName,
                    StoragePath = DocumentStep.BuildStoragePath(storageName, tenantId),
                    Extension = Fit(context, "Document", "Extension", ExtensionOf(name)),
                    ContentType = Fit(context, "Document", "ContentType", Text(reader, "DosyaTuru")),
                    SizeBytes = Long(reader, "Boyut") ?? 0,
                    OwnerType = DocumentOwnerType.FieldObservationReport,
                    OwnerRecordId = Lookup(reportMap, Int(reader, "SahaGozlemRaporuId")),
                    IsActive = true,
                    CreationTime = DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> FieldObservationLinesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var reportMap = await context.IdMap.LoadAsync("SahaGozlemRaporu_T", cancellationToken);
        var blobMap = await context.IdMap.LoadAsync(FieldObservationBlobs, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<FieldObservationLine>(
            context, "SahaGozlemRaporuSatirlari_T", "field observation lines",
            """
            SELECT SahaGozlemSatiriId, SahaGozlemRaporuId, Tarih, TerminTarihi, Uygunsuzluk,
                   Onlemler, Sorumlu, Risk, KurumId
            FROM SahaGozlemRaporuSatirlari_T ORDER BY SahaGozlemSatiriId;
            """,
            "SahaGozlemSatiriId",
            (reader, orphan) =>
            {
                if (!reportMap.TryGetValue(Required(reader, "SahaGozlemRaporuId"), out var reportId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new FieldObservationLine
                {
                    FieldObservationReportId = reportId,
                    Date = Date(reader, "Tarih"),
                    DeadlineDate = Date(reader, "TerminTarihi"),
                    NonConformity =
                        Fit(context, "FieldObservationLine", "NonConformity", Text(reader, "Uygunsuzluk"))
                        ?? string.Empty,
                    Measures = Fit(context, "FieldObservationLine", "Measures", Text(reader, "Onlemler")),

                    // Sorumlu is free text in the legacy table — a name typed into a box, not a
                    // reference. It stays text; OwnerCompanyEmployeeId is for records that name
                    // an actual employee, and guessing one from a string would invent a link.
                    Owner = Fit(context, "FieldObservationLine", "Owner", Text(reader, "Sorumlu")),
                    OwnerCompanyEmployeeId = null,

                    RiskCategory = RiskCategoryOf(Text(reader, "Risk")),
                    DocumentId = Lookup(blobMap, Required(reader, "SahaGozlemSatiriId")),
                    CreationTime = Date(reader, "Tarih") ?? DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ corrective actions

    private static async Task<StepResult> CorrectiveActionsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var lineMap = await context.IdMap.LoadAsync("SahaGozlemRaporuSatirlari_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CorrectiveAction>(
            context, "DOF_T", "corrective actions",
            """
            SELECT DofId, FirmaId, KurumId, Tespit, Oneri, Sonuc, Kaynak, TDosyaId, SDosyaId,
                   Risk, IslemSonucu, Sorumlu, TespitTarihi, TerminTarihi, SonucTarihi,
                   SahaGozlemSatiriId, IsDeleted, EklemeTarihi, GuncellemeTarihi
            FROM DOF_T ORDER BY DofId;
            """,
            "DofId",
            (reader, orphan) =>
            {
                if (Int(reader, "FirmaId") is not int legacyCompanyId
                    || !companyMap.TryGetValue(legacyCompanyId, out var companyId)
                    || Int(reader, "KurumId") is not int legacyTenantId
                    || !organizationMap.TryGetValue(legacyTenantId, out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new CorrectiveAction
                {
                    CompanyId = companyId,
                    Finding = Fit(context, "CorrectiveAction", "Finding", Text(reader, "Tespit")) ?? string.Empty,
                    Recommendation = Fit(context, "CorrectiveAction", "Recommendation", Text(reader, "Oneri")),
                    Result = Fit(context, "CorrectiveAction", "Result", Text(reader, "Sonuc")),
                    Source = Fit(context, "CorrectiveAction", "Source", Text(reader, "Kaynak")),
                    FindingDocumentId = Lookup(documentMap, Int(reader, "TDosyaId")),
                    ResultDocumentId = Lookup(documentMap, Int(reader, "SDosyaId")),
                    RiskCategory = RiskCategoryOf(Text(reader, "Risk")),
                    OperationResult = StatusOf(Int(reader, "IslemSonucu")),
                    Owner = Fit(context, "CorrectiveAction", "Owner", Text(reader, "Sorumlu")),
                    OwnerCompanyEmployeeId = null,
                    FindingDate = Date(reader, "TespitTarihi"),
                    DeadlineDate = Date(reader, "TerminTarihi"),
                    ResultDate = Date(reader, "SonucTarihi"),
                    FieldObservationLineId = Lookup(lineMap, Int(reader, "SahaGozlemSatiriId")),
                    IsDeleted = Bit(reader, "IsDeleted"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ incidents

    private static async Task<StepResult> IncidentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var departmentMap = await context.IdMap.LoadAsync("IsyeriBolum_T", cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<Incident>(
            context, "Olay_T", "incidents",
            """
            SELECT OlayId, FirmaId, BolumId, OlayTuru, KazaTuru, OlayTarihi, Aciklama, Ifade,
                   GDosyaId, BirimAmirId, AmirAdSoyad, KurumId, IsDeleted,
                   EklemeTarihi, GuncellemeTarihi
            FROM Olay_T ORDER BY OlayId;
            """,
            "OlayId",
            (reader, orphan) =>
            {
                // The department is required by the rebuilt schema, and rightly: an accident
                // happened somewhere. A row whose department did not survive cannot be placed.
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !departmentMap.TryGetValue(Required(reader, "BolumId"), out var departmentId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new Incident
                {
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    IncidentType = IncidentTypeOf(Int(reader, "OlayTuru")),

                    // The legacy system records severity, not mechanism. See AccidentSeverity.
                    AccidentType = AccidentType.Unspecified,
                    AccidentSeverity = SeverityOf(Int(reader, "KazaTuru")),

                    IncidentDate = Date(reader, "OlayTarihi") ?? DateTime.Now,
                    Description = Fit(context, "Incident", "Description", Text(reader, "Aciklama")),
                    Expression = Fit(context, "Incident", "Expression", Text(reader, "Ifade")),
                    DocumentId = Lookup(documentMap, Int(reader, "GDosyaId")),
                    UnitSupervisorId = Lookup(employeeMap, Int(reader, "BirimAmirId")),
                    SupervisorFullName =
                        Fit(context, "Incident", "SupervisorFullName", Text(reader, "AmirAdSoyad")),

                    // Neither the lost-day count nor the SGK notification date exists in the
                    // legacy schema. The severity bands imply a range, not a number, and inventing
                    // one would misreport a statutory figure.
                    LostWorkDays = null,
                    SsiNotificationDate = null,

                    IsDeleted = Bit(reader, "IsDeleted"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> IncidentPeopleAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var incidentMap = await context.IdMap.LoadAsync("Olay_T", cancellationToken);
        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);

        // OlayKisi_T carries no KurumId; the tenant is the incident's.
        var incidentTenants = await LoadTenantsAsync(context, "ensa.Incident", cancellationToken);

        return await CopyAsync<IncidentPerson>(
            context, "OlayKisi_T", "incident people",
            "SELECT OlayKisiId, OlayId, PersonelId, Adi, Soyadi, KisiTur FROM OlayKisi_T ORDER BY OlayKisiId;",
            "OlayKisiId",
            (reader, orphan) =>
            {
                if (!incidentMap.TryGetValue(Required(reader, "OlayId"), out var incidentId))
                {
                    orphan();
                    return null;
                }

                return new IncidentPerson
                {
                    IncidentId = incidentId,
                    PersonType = PersonRoleOf(Int(reader, "KisiTur")),
                    CompanyEmployeeId = Lookup(employeeMap, Int(reader, "PersonelId")),
                    Name = Fit(context, "IncidentPerson", "Name", Text(reader, "Adi")) ?? string.Empty,
                    LastName = Fit(context, "IncidentPerson", "LastName", Text(reader, "Soyadi")) ?? string.Empty,
                    CreationTime = DateTime.Now,
                    TenantId = incidentTenants.TryGetValue(incidentId, out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ emergency action plans

    private static async Task<StepResult> EvacuationPlanBlobsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<Document>(
            context, EvacuationPlanBlobs, "evacuation plans",
            """
            SELECT AcilDurumEylemPlaniId, KurumId, CAST(DATALENGTH(TahliyePlani) AS bigint) AS Boyut
            FROM AcilDurumEylemPlani_T
            WHERE DATALENGTH(TahliyePlani) > 0
            ORDER BY AcilDurumEylemPlaniId;
            """,
            "AcilDurumEylemPlaniId",
            (reader, orphan) =>
            {
                if (!organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                var legacyId = Required(reader, "AcilDurumEylemPlaniId");
                var storageName = DocumentStep.DeriveStorageName(EvacuationPlanBlobs, legacyId);

                return new Document
                {
                    // The legacy column carries no name or content type — it is an image column
                    // with nothing beside it — so the name says what the file is and the type is
                    // left unset rather than assumed.
                    DocumentName = $"tahliye-plani-{legacyId}",
                    StorageName = storageName,
                    StoragePath = DocumentStep.BuildStoragePath(storageName, tenantId),
                    SizeBytes = Long(reader, "Boyut") ?? 0,
                    OwnerType = DocumentOwnerType.EmergencyActionPlan,
                    IsActive = true,
                    CreationTime = DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> EmergencyPlansAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var evacuationMap = await context.IdMap.LoadAsync(EvacuationPlanBlobs, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<EmergencyActionPlan>(
            context, "AcilDurumEylemPlani_T", "emergency action plans",
            """
            SELECT AcilDurumEylemPlaniId, FirmaId, KurumId, HazirlanmaTarihi, GecerlilikTarihi,
                   FirmaAdi, Adres, SicilNo, TehlikeSinifi, Telefon, EkiplerSefi, AcilDurumEkibi,
                   CalisanTemsilcisi, DestekElemani, IsverenVeyaVekili, IsGuvenligiUzmani,
                   IsyeriDoktoru, KorumaPersoneli, EklemeTarihi, GuncellemeTarihi
            FROM AcilDurumEylemPlani_T ORDER BY AcilDurumEylemPlaniId;
            """,
            "AcilDurumEylemPlaniId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new EmergencyActionPlan
                {
                    CompanyId = companyId,
                    PreparedDate = Date(reader, "HazirlanmaTarihi") ?? DateTime.Now,
                    ValidityDate = Date(reader, "GecerlilikTarihi") ?? DateTime.Now,
                    CompanyName = Fit(context, "EmergencyActionPlan", "CompanyName", Text(reader, "FirmaAdi")),
                    Address = Fit(context, "EmergencyActionPlan", "Address", Text(reader, "Adres")),
                    RegistrationNo = Fit(context, "EmergencyActionPlan", "RegistrationNo", Text(reader, "SicilNo")),
                    HazardClass = HazardClassOf(Text(reader, "TehlikeSinifi")),
                    Phone = Fit(context, "EmergencyActionPlan", "Phone", Text(reader, "Telefon")),
                    TeamsChief = Fit(context, "EmergencyActionPlan", "TeamsChief", Text(reader, "EkiplerSefi")),
                    EmergencyTeam = Fit(context, "EmergencyActionPlan", "EmergencyTeam", Text(reader, "AcilDurumEkibi")),
                    WorkerRepresentative =
                        Fit(context, "EmergencyActionPlan", "WorkerRepresentative", Text(reader, "CalisanTemsilcisi")),
                    SupportStaff = Fit(context, "EmergencyActionPlan", "SupportStaff", Text(reader, "DestekElemani")),
                    EmployerOrDeputy =
                        Fit(context, "EmergencyActionPlan", "EmployerOrDeputy", Text(reader, "IsverenVeyaVekili")),
                    OccupationalSafetySpecialist =
                        Fit(context, "EmergencyActionPlan", "OccupationalSafetySpecialist", Text(reader, "IsGuvenligiUzmani")),
                    WorkplacePhysician =
                        Fit(context, "EmergencyActionPlan", "WorkplacePhysician", Text(reader, "IsyeriDoktoru")),
                    ProtectionEmployee =
                        Fit(context, "EmergencyActionPlan", "ProtectionEmployee", Text(reader, "KorumaPersoneli")),
                    EvacuationPlanDocumentId =
                        Lookup(evacuationMap, Required(reader, "AcilDurumEylemPlaniId")),

                    // AcilDurumEylemPlani_T.Dosya exists but is empty in all 3,906 rows.
                    DocumentId = null,

                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// The nine narrative columns of a plan become nine <see cref="EmergencyPlanSection"/> rows.
    /// <para>
    /// The legacy table keeps each section in its own <c>nvarchar(max)</c> column, which is why
    /// adding a tenth section meant a schema change. Written as rows, only the sections that
    /// actually have content: an empty column is a section nobody filled in, not an empty section.
    /// </para>
    /// <para>
    /// This is the one part of the step that is not one legacy row to one modern row, so it
    /// cannot use the shared copy: the id map is keyed on a single legacy id, and nine rows share
    /// one. A re-run therefore checks the destination instead.
    /// </para>
    /// </summary>
    private static async Task<StepResult> EmergencyPlanSectionsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        (string Column, EmergencyPlanSectionType Type)[] sections =
        [
            ("Icindekiler", EmergencyPlanSectionType.TableOfContents),
            ("Giris", EmergencyPlanSectionType.Introduction),
            ("OrganizasyondaYeralanEkiplerVeSorumluluklari", EmergencyPlanSectionType.OrganizationAndResponsibilities),
            ("Talimatlar", EmergencyPlanSectionType.Instructions),
            ("Savas", EmergencyPlanSectionType.Wartime),
            ("AcilDurumTatbikatiUygulamasi", EmergencyPlanSectionType.DrillProcedure),
            ("YanginKontrolForumu", EmergencyPlanSectionType.FireControlForm),
            ("IlkYardim", EmergencyPlanSectionType.FirstAid),
            ("AcilDurumTelefonlari", EmergencyPlanSectionType.EmergencyPhones),
        ];

        await using var db = context.CreateDbContext();

        var planMap = await context.IdMap.LoadAsync("AcilDurumEylemPlani_T", cancellationToken);

        var alreadyWritten = await db.Set<EmergencyPlanSection>()
            .Select(s => s.EmergencyActionPlanId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var done = alreadyWritten.ToHashSet();

        var read = 0;
        var written = 0;
        var batch = new List<EmergencyPlanSection>();

        var sql = $"""
            SELECT AcilDurumEylemPlaniId, {string.Join(", ", sections.Select(s => s.Column))}
            FROM AcilDurumEylemPlani_T ORDER BY AcilDurumEylemPlaniId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (!planMap.TryGetValue(Required(reader, "AcilDurumEylemPlaniId"), out var planId)
                    || done.Contains(planId))
                {
                    continue;
                }

                var order = 0;
                foreach (var (column, type) in sections)
                {
                    order++;

                    if (Text(reader, column) is not { } content)
                    {
                        continue;
                    }

                    batch.Add(new EmergencyPlanSection
                    {
                        EmergencyActionPlanId = planId,
                        SectionType = type,
                        Content = content,
                        OrderNo = order,
                        CreationTime = DateTime.Now,
                    });
                }

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    db.Set<EmergencyPlanSection>().AddRange(batch);
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
                db.Set<EmergencyPlanSection>().AddRange(batch);
                await db.SaveChangesAsync(cancellationToken);
            }

            written += batch.Count;
            batch.Clear();
        }

        var note = $"emergency plan sections: {written} written";
        if (read > 0 && written == 0)
        {
            // Not a failure: all nine narrative columns are empty in all 3,906 legacy plans. The
            // plans were produced as documents and the sections were never typed into the form.
            note += " (all nine section columns are empty in every legacy plan)";
        }

        return new StepResult(read, written, 0, note);
    }

    private static async Task<StepResult> EmergencyTeamMembersAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var planMap = await context.IdMap.LoadAsync("AcilDurumEylemPlani_T", cancellationToken);
        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<EmergencyTeamMember>(
            context, "AcilDurumEylemPlaniPersoneli_T", "emergency team members",
            """
            SELECT AcilDurumEylemPlaniPersoneliId, AcilDurumEylemPlaniId, FirmaPersonelId,
                   PersonelTuru, EkipTuru, KurumId
            FROM AcilDurumEylemPlaniPersoneli_T ORDER BY AcilDurumEylemPlaniPersoneliId;
            """,
            "AcilDurumEylemPlaniPersoneliId",
            (reader, orphan) =>
            {
                if (!planMap.TryGetValue(Required(reader, "AcilDurumEylemPlaniId"), out var planId)
                    || !employeeMap.TryGetValue(Required(reader, "FirmaPersonelId"), out var employeeId))
                {
                    orphan();
                    return null;
                }

                return new EmergencyTeamMember
                {
                    EmergencyActionPlanId = planId,
                    CompanyEmployeeId = employeeId,

                    // PersonelTuru is "asil" in all 7,330 rows — principal member as opposed to
                    // substitute. That is not what StaffRole asks (safety specialist, physician),
                    // and the rebuilt schema records no substitute flag, so nothing is claimed.
                    StaffRole = StaffRole.Unspecified,

                    TeamType = TeamTypeOf(Text(reader, "EkipTuru")),
                    CreationTime = DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId)
                        ? tenantId
                        : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ value mapping

    /// <summary>
    /// <c>Risk</c> is free text in both <c>DOF_T</c> and the observation lines, offered by a
    /// dropdown with two options and holding <c>-1</c>, <c>0</c>, <c>1</c> and blanks besides.
    /// Matching is on folded text so the Turkish diacritics in the stored values do not decide it.
    /// </summary>
    private static RiskCategory RiskCategoryOf(string? risk)
    {
        var folded = Fold(risk);

        if (folded is null)
        {
            return RiskCategory.Unspecified;
        }

        if (folded.Contains("KAZA", StringComparison.Ordinal))
        {
            return RiskCategory.WorkAccidentRisk;
        }

        return folded.Contains("MESLEK", StringComparison.Ordinal)
            ? RiskCategory.OccupationalDiseaseRisk
            : RiskCategory.Unspecified;
    }

    /// <summary>
    /// <c>DOF_T.IslemSonucu</c>. The legacy form offers 0 "Uygulamada", 1 "Kapatildi" and -1
    /// "Iptal Edildi", and 2 as its "choose one" placeholder — which therefore means nothing was
    /// chosen, not a fourth state.
    /// </summary>
    private static CorrectiveActionStatus StatusOf(int? status)
        => status switch
        {
            1 => CorrectiveActionStatus.Closed,
            -1 => CorrectiveActionStatus.Cancelled,
            _ => CorrectiveActionStatus.InProgress,
        };

    /// <summary><c>Olay_T.OlayTuru</c>: 0 "Is Kazasi", 1 "Ramak Kala Olayi".</summary>
    private static IncidentType IncidentTypeOf(int? type)
        => type == 1 ? IncidentType.NearMiss : IncidentType.WorkAccident;

    /// <summary>
    /// <c>Olay_T.KazaTuru</c>, the legacy severity scale, in the order its form lists it.
    /// </summary>
    private static AccidentSeverity SeverityOf(int? severity)
        => severity switch
        {
            0 => AccidentSeverity.NarrowlyAvoided,
            1 => AccidentSeverity.UpToThreeLostDays,
            2 => AccidentSeverity.MoreThanThreeLostDays,
            3 => AccidentSeverity.LimbLoss,
            4 => AccidentSeverity.Disablement,
            5 => AccidentSeverity.Fatal,
            6 => AccidentSeverity.PropertyDamage,
            _ => AccidentSeverity.Unspecified,
        };

    /// <summary>
    /// <c>OlayKisi_T.KisiTur</c>: 1 is the person the accident happened to, 0 a witness. Taken
    /// from the legacy read path, which splits the same list on exactly that test.
    /// </summary>
    private static IncidentPersonRole PersonRoleOf(int? role)
        => role == 1 ? IncidentPersonRole.Affected : IncidentPersonRole.Witness;

    /// <summary><c>AcilDurumEylemPlani_T.TehlikeSinifi</c>, stored as its Turkish label.</summary>
    private static HazardClass HazardClassOf(string? hazardClass)
    {
        var folded = Fold(hazardClass);

        if (folded is null)
        {
            return HazardClass.Unspecified;
        }

        // "COK TEHLIKELI" contains "TEHLIKELI", so the most specific test comes first.
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

    /// <summary><c>AcilDurumEylemPlaniPersoneli_T.EkipTuru</c>, one of four lower-case slugs.</summary>
    private static EmergencyTeamType TeamTypeOf(string? team)
        => Fold(team) switch
        {
            "YANGINSONDURME" => EmergencyTeamType.FireFighting,
            "ARAMAKURTARMA" => EmergencyTeamType.RescueAndEvacuation,
            "ILKYARDIM" => EmergencyTeamType.FirstAid,
            "KORUMA" => EmergencyTeamType.Protection,
            _ => EmergencyTeamType.Unspecified,
        };

    /// <summary>
    /// Upper case, without Turkish diacritics, so a comparison does not turn on whether the value
    /// was typed with a dotted or dotless i.
    /// </summary>
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

    /// <summary>The tenant of every row already in a destination table, by id.</summary>
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

    private static string? ExtensionOf(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        if (dot < 0 || dot == fileName.Length - 1)
        {
            return null;
        }

        var extension = fileName[(dot + 1)..].Trim().ToLowerInvariant();
        return extension.Length is > 0 and <= 16 ? extension : null;
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

    private static long? Long(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToInt64(reader.GetValue(index));
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
