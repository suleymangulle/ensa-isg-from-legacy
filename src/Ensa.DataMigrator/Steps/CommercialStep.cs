using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Communication;
using Ensa.Domain.Companies;
using Ensa.Domain.Documents;
using Ensa.Domain.Finance;
using Ensa.Domain.Menus;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The commercial and administrative tail: contracts and prospects, the penalty calculator, module
/// subscriptions and archives, distance tables, internal messages and the risk incident history.
/// <para>
/// These are the tables nothing else depends on, which is why they come last — and also why they
/// are the ones a migration quietly forgets. Each is small; together they are eleven screens that
/// would otherwise open empty.
/// </para>
/// </summary>
public sealed class CommercialStep : IMigrationStep
{
    public int Order => 110;

    public string Name => "commercial";

    public string Description => "Contracts, prospects, penalties, module archives, distances, messages and risk incident history";

    private const int BatchSize = 500;

    /// <summary>The id map key for the module archive files, shared with <c>--export-documents</c>.</summary>
    public const string ModuleArchiveBlobs = "ModulArsivDetay_T:Dosya";

    /// <summary>The id map key for the penalty survey logos.</summary>
    public const string PenaltySurveyLogoBlobs = "CezaAnketi_T:Logo";

    /// <summary>
    /// The legacy <c>PaketTuru</c> and <c>KurumTuru</c> codes, mapped the way
    /// <see cref="TenancyStep"/> already maps them. Repeated rather than shared so the two are
    /// visibly the same decision; a contract filed under a different plan from the organization it
    /// created would be worse than either mapping alone.
    /// </summary>
    private static readonly Dictionary<string, string> SubscriptionPlanCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pro"] = "PROFESYONEL",
            ["demo"] = "DEMO",
            ["startup"] = "BASLANGIC",
            ["ensa"] = "KURUMSAL",
        };

    private static readonly Dictionary<string, string> OrganizationTypeCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["OSGB"] = "OSGB",
            ["Bireysel"] = "BIREYSEL",
            ["Kurumsal"] = "ISGB",
            ["ensa"] = "OSGB",
        };

    /// <summary>The four legacy risk incident tables, which differ only in what they record.</summary>
    private static readonly (string Table, string KeyColumn, RiskHistoryRecordType Type)[] RiskHistories =
    [
        ("RiskIsKazasiKayit_T", "RiskIsKazasiKayitId", RiskHistoryRecordType.WorkAccident),
        ("RiskHasarsizIsKazasiKayit_T", "RiskHasarsizIsKazasiKayit", RiskHistoryRecordType.NoDamageWorkAccident),
        ("RiskMeslekHastaliklariKayit_T", "RiskMeslekHastaliklariKayitId", RiskHistoryRecordType.OccupationalDisease),
        ("RiskRamakKalaOlayKayit_T", "RiskRamakKalaOlayKayitId", RiskHistoryRecordType.NearMissIncident),
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await CompanyModulesAsync(context, cancellationToken),
            await CompanyActivitiesAsync(context, cancellationToken),
            await RouteOriginsAsync(context, cancellationToken),
            await RouteOriginDistancesAsync(context, cancellationToken),
            await ModuleArchivesAsync(context, cancellationToken),
            await ModuleArchiveBlobsAsync(context, cancellationToken),
            await ModuleArchiveItemsAsync(context, cancellationToken),
            await MessagesAsync(context, cancellationToken),
            await PenaltiesAsync(context, cancellationToken),
            await PenaltyAmountsAsync(context, cancellationToken),
            await PenaltySurveyLogosAsync(context, cancellationToken),
            await PenaltySurveysAsync(context, cancellationToken),
            await PenaltySurveyLinesAsync(context, cancellationToken),
            await ContractsAsync(context, cancellationToken),
            await ProspectsAsync(context, cancellationToken),
        };

        foreach (var (table, keyColumn, type) in RiskHistories)
        {
            results.Add(await RiskHistoryAsync(context, table, keyColumn, type, cancellationToken));
        }

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ company links

    private static async Task<StepResult> CompanyModulesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var moduleMap = await context.IdMap.LoadAsync("Modul_T", cancellationToken);
        var companyTenants = await LoadTenantsAsync(context, "ensa.Company", cancellationToken);

        return await CopyAsync<CompanyModule>(
            context, "FirmaModulBaglanti_T", "company modules",
            "SELECT BaglantiId, ModulId, FirmaId, Aktif, EklemeTarihi, GuncellemeTarihi FROM FirmaModulBaglanti_T ORDER BY BaglantiId;",
            "BaglantiId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !moduleMap.TryGetValue(Required(reader, "ModulId"), out var moduleId))
                {
                    orphan();
                    return null;
                }

                return new CompanyModule
                {
                    CompanyId = companyId,
                    ModuleId = moduleId,
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = companyTenants.GetValueOrDefault(companyId),
                };
            },
            cancellationToken,

            // The key is the DESTINATION's, not the legacy row's: the index is on the modern
            // company and module ids, and a key built from legacy ids would never match what the
            // destination already holds - leaving the seeding useless and a resumed run colliding.
            reader => (companyMap.TryGetValue(Required(reader, "FirmaId"), out var company) ? company : 0)
                      + "|" + (moduleMap.TryGetValue(Required(reader, "ModulId"), out var module) ? module : 0),
            link => link.CompanyId + "|" + link.ModuleId);
    }

    private static async Task<StepResult> CompanyActivitiesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        await using var db = context.CreateDbContext();
        var activities = await db.Set<Ensa.Domain.Plans.Activity>()
            .Select(a => new { a.Id, a.ActivityCode })
            .ToListAsync(cancellationToken);

        var byCode = activities
            .Where(a => a.ActivityCode != null)
            .GroupBy(a => a.ActivityCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        return await CopyAsync<CompanyActivity>(
            context, "FirmaAktivite_T", "company activities",
            "SELECT FirmaAktiviteId, FirmaId, AktiviteKodu, KurumId, EklemeTarihi FROM FirmaAktivite_T ORDER BY FirmaAktiviteId;",
            "FirmaAktiviteId",
            (reader, orphan) =>
            {
                var code = Text(reader, "AktiviteKodu");

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || code is null
                    || !byCode.TryGetValue(code, out var activityId))
                {
                    orphan();
                    return null;
                }

                return new CompanyActivity
                {
                    CompanyId = companyId,
                    ActivityId = activityId,
                    ActivityCode = Fit(context, "CompanyActivity", "ActivityCode", code),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ distances

    private static async Task<StepResult> RouteOriginsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var cityMap = await context.IdMap.LoadAsync("Sehir_T", cancellationToken);
        var districtMap = await context.IdMap.LoadAsync("Ilce_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<RouteOrigin>(
            context, "FirmaOrgin_T", "route origins",
            "SELECT OrginId, Tanim, SehirId, IlceId, Adres, KurumId, EklemeTarihi FROM FirmaOrgin_T ORDER BY OrginId;",
            "OrginId",
            (reader, orphan) =>
            {
                if (!cityMap.TryGetValue(Required(reader, "SehirId"), out var cityId))
                {
                    orphan();
                    return null;
                }

                return new RouteOrigin
                {
                    Tag = Fit(context, "RouteOrigin", "Tag", Text(reader, "Tanim")),
                    CityId = cityId,
                    DistrictId = Lookup(districtMap, Int(reader, "IlceId")),
                    Address = Fit(context, "RouteOrigin", "Address", Text(reader, "Adres")),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> RouteOriginDistancesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var originMap = await context.IdMap.LoadAsync("FirmaOrgin_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<RouteOriginDistance>(
            context, "OrginFirmaMesafe_T", "route distances",
            "SELECT OrginFirmaId, OrginId, SehirAdi, FirmaId, MesafeKm, KurumId, EklemeTarihi FROM OrginFirmaMesafe_T ORDER BY OrginFirmaId;",
            "OrginFirmaId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    orphan();
                    return null;
                }

                return new RouteOriginDistance
                {
                    OriginId = Lookup(originMap, Int(reader, "OrginId")),
                    CityName = Fit(context, "RouteOriginDistance", "CityName", Text(reader, "SehirAdi")),
                    CompanyId = companyId,
                    DistanceKm = Money(reader, "MesafeKm"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ module archives

    private static async Task<StepResult> ModuleArchivesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<ModuleArchive>(
            context, "ModulArsiv_T", "module archives",
            "SELECT ModulArsivId, ModulAdi, ModulKodu, KurumId, ModulEklemeTarihi FROM ModulArsiv_T ORDER BY ModulArsivId;",
            "ModulArsivId",
            (reader, _) => new ModuleArchive
            {
                ModuleName = Fit(context, "ModuleArchive", "ModuleName", Text(reader, "ModulAdi")) ?? string.Empty,
                ModuleCode = Fit(context, "ModuleArchive", "ModuleCode", Text(reader, "ModulKodu")) ?? string.Empty,
                CreationTime = Date(reader, "ModulEklemeTarihi") ?? DateTime.Now,
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken);
    }

    /// <summary>
    /// The 519 MB of archived reports the legacy schema keeps inline, as <see cref="Document"/>
    /// rows. Metadata here, payload by <c>--export-documents</c>, like every other file.
    /// </summary>
    private static async Task<StepResult> ModuleArchiveBlobsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<Document>(
            context, ModuleArchiveBlobs, "archived report files",
            """
            SELECT ModulArsivDetayId, DosyaAdi, DosyaTuru, KurumId,
                   CAST(DATALENGTH(Dosya) AS bigint) AS Boyut
            FROM ModulArsivDetay_T WHERE DATALENGTH(Dosya) > 0 ORDER BY ModulArsivDetayId;
            """,
            "ModulArsivDetayId",
            (reader, orphan) =>
            {
                if (!organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                var legacyId = Required(reader, "ModulArsivDetayId");
                var name = Text(reader, "DosyaAdi") ?? $"arsiv-{legacyId}";
                var storageName = DocumentStep.DeriveStorageName(ModuleArchiveBlobs, legacyId);

                return new Document
                {
                    DocumentName = Fit(context, "Document", "DocumentName", name) ?? $"arsiv-{legacyId}",
                    StorageName = storageName,
                    StoragePath = DocumentStep.BuildStoragePath(storageName, tenantId),
                    Extension = Fit(context, "Document", "Extension", ExtensionOf(name)),
                    ContentType = Fit(context, "Document", "ContentType", Text(reader, "DosyaTuru")),
                    SizeBytes = Long(reader, "Boyut") ?? 0,
                    IsActive = true,
                    CreationTime = DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> ModuleArchiveItemsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var archiveMap = await context.IdMap.LoadAsync("ModulArsiv_T", cancellationToken);
        var officeMap = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);
        var blobMap = await context.IdMap.LoadAsync(ModuleArchiveBlobs, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<ModuleArchiveItem>(
            context, "ModulArsivDetay_T", "archived report items",
            """
            SELECT ModulArsivDetayId, ModulArsivId, OfisId, KurumId, EklemeTarihi, GuncellemeTarihi
            FROM ModulArsivDetay_T ORDER BY ModulArsivDetayId;
            """,
            "ModulArsivDetayId",
            (reader, orphan) =>
            {
                // The document is required: an archive item is a stored report, and one with no
                // file behind it is a claim that a report was archived when it was not.
                if (!archiveMap.TryGetValue(Required(reader, "ModulArsivId"), out var archiveId)
                    || !officeMap.TryGetValue(Required(reader, "OfisId"), out var officeId)
                    || !blobMap.TryGetValue(Required(reader, "ModulArsivDetayId"), out var documentId))
                {
                    orphan();
                    return null;
                }

                return new ModuleArchiveItem
                {
                    ModuleArchiveId = archiveId,
                    OfficeId = officeId,
                    DocumentId = documentId,
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ messages

    private static async Task<StepResult> MessagesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userTenants = await LoadTenantsAsync(context, "ensa.[User]", cancellationToken);

        return await CopyAsync<Message>(
            context, "Mesajlasma_T", "messages",
            """
            SELECT MesajId, Mesaj, GonderimTarihi, MesajTip, AliciId, GondericiId, FirmaId,
                   Okundu, OkunmaTarihi
            FROM Mesajlasma_T ORDER BY MesajId;
            """,
            "MesajId",
            (reader, orphan) =>
            {
                // Which table an id points at depends on the message type, and the destination's
                // own enum says so: an EmployeeSenderMessage was sent by an employee, an
                // EmployeeRecipientMessage was sent to one. Reading every id as a user id loses
                // the 71 messages that are not, and silently attributes the ones that collide.
                var messageType = MessageTypeOf(Int(reader, "MesajTip"));

                var senderMap = messageType == MessageType.EmployeeSenderMessage ? employeeMap : userMap;
                var recipientMap = messageType == MessageType.EmployeeRecipientMessage ? employeeMap : userMap;

                if (!recipientMap.TryGetValue(Required(reader, "AliciId"), out var recipientId)
                    || !senderMap.TryGetValue(Required(reader, "GondericiId"), out var senderId))
                {
                    orphan();
                    return null;
                }

                return new Message
                {
                    MessageType = messageType,
                    Content = Fit(context, "Message", "Content", Text(reader, "Mesaj")) ?? string.Empty,
                    RecipientId = recipientId,
                    SenderId = senderId,
                    CompanyId = Lookup(companyMap, Int(reader, "FirmaId")),
                    IsRead = Bit(reader, "Okundu"),
                    ReadDate = Date(reader, "OkunmaTarihi"),
                    CreationTime = Date(reader, "GonderimTarihi") ?? DateTime.Now,

                    // From whichever end is a user: an employee has no tenant of its own to read.
                    TenantId = messageType == MessageType.EmployeeSenderMessage
                        ? userTenants.GetValueOrDefault(recipientId)
                        : userTenants.GetValueOrDefault(senderId),
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ penalties

    private static Task<StepResult> PenaltiesAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<Penalty>(
            context, "Ceza_T", "penalties",
            """
            SELECT CezaId, TreeItemCode, KanunMaddesi, CezaMaddesi, KanunMaddesindeSozuEdilenFiil,
                   CarpanHesapla, Aktif, IsDeleted, EklemeTarihi, GuncellemeTarihi
            FROM Ceza_T ORDER BY CezaId;
            """,
            "CezaId",
            (reader, _) => new Penalty
            {
                TreeNodeCode = Fit(context, "Penalty", "TreeNodeCode", Text(reader, "TreeItemCode")),
                LawArticle = Fit(context, "Penalty", "LawArticle", Text(reader, "KanunMaddesi")) ?? string.Empty,
                PenaltyArticle = Fit(context, "Penalty", "PenaltyArticle", Text(reader, "CezaMaddesi")) ?? string.Empty,
                LawArticleReferencedOffence =
                    Fit(context, "Penalty", "LawArticleReferencedOffence", Text(reader, "KanunMaddesindeSozuEdilenFiil")),
                MultiplierCalculate = Bit(reader, "CarpanHesapla"),
                IsActive = Bit(reader, "Aktif"),
                IsDeleted = Bit(reader, "IsDeleted"),
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
            },
            cancellationToken);

    /// <summary>
    /// The nine-column penalty matrix becomes nine rows: three hazard classes by three employee
    /// count bands.
    /// <para>
    /// <b>The year is the one the row was entered in.</b> Turkish administrative fines are
    /// revalued every year and the legacy table holds one matrix with no year on it, so the only
    /// honest reading is that the amounts are the ones in force when somebody typed them. Recording
    /// that year is what lets a later year's figures be added beside these rather than over them.
    /// </para>
    /// </summary>
    private static async Task<StepResult> PenaltyAmountsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var penaltyMap = await context.IdMap.LoadAsync("Ceza_T", cancellationToken);
        if (penaltyMap.Count == 0)
        {
            return new StepResult(0, 0, 0, null);
        }

        var done = (await db.Set<PenaltyAmount>()
            .Select(a => a.PenaltyId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        (string Column, HazardClass Class, EmployeeCountRange Range)[] cells =
        [
            ("AzTehlikeli_K_10_Ceza", HazardClass.LowHazard, EmployeeCountRange.FewerThanTen),
            ("AzTehlikeli_10_ve_49_Ceza", HazardClass.LowHazard, EmployeeCountRange.TenToFortyNine),
            ("AzTehlikeli_BE_50", HazardClass.LowHazard, EmployeeCountRange.FiftyOrMore),
            ("Tehlikeli_K_10_Ceza", HazardClass.Hazardous, EmployeeCountRange.FewerThanTen),
            ("Tehlikeli_10_ve_49_Ceza", HazardClass.Hazardous, EmployeeCountRange.TenToFortyNine),
            ("Tehlikeli_BE_50", HazardClass.Hazardous, EmployeeCountRange.FiftyOrMore),
            ("CokTehlikeli_K_10_Ceza", HazardClass.VeryHazardous, EmployeeCountRange.FewerThanTen),
            ("CokTehlikeli_10_ve_49_Ceza", HazardClass.VeryHazardous, EmployeeCountRange.TenToFortyNine),
            ("CokTehlikeli_BE_50", HazardClass.VeryHazardous, EmployeeCountRange.FiftyOrMore),
        ];

        var read = 0;
        var batch = new List<PenaltyAmount>();

        var sql = $"""
            SELECT CezaId, EklemeTarihi, {string.Join(", ", cells.Select(c => c.Column))}
            FROM Ceza_T ORDER BY CezaId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (!penaltyMap.TryGetValue(Required(reader, "CezaId"), out var penaltyId) || done.Contains(penaltyId))
                {
                    continue;
                }

                var year = (Date(reader, "EklemeTarihi") ?? DateTime.Now).Year;

                foreach (var (column, hazardClass, range) in cells)
                {
                    batch.Add(new PenaltyAmount
                    {
                        PenaltyId = penaltyId,
                        HazardClass = hazardClass,
                        EmployeeCountRange = range,
                        Amount = Money(reader, column),
                        ValidityYear = year,
                        CreationTime = DateTime.Now,
                    });
                }
            }
        }

        if (!context.DryRun && batch.Count > 0)
        {
            db.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new StepResult(read, batch.Count, 0, $"penalty amounts: {batch.Count} written");
    }

    private static async Task<StepResult> PenaltySurveyLogosAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<Document>(
            context, PenaltySurveyLogoBlobs, "penalty survey logos",
            """
            SELECT CezaAnketId, KurumId, CAST(DATALENGTH(Logo) AS bigint) AS Boyut
            FROM CezaAnketi_T WHERE DATALENGTH(Logo) > 0 ORDER BY CezaAnketId;
            """,
            "CezaAnketId",
            (reader, orphan) =>
            {
                if (!organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                var legacyId = Required(reader, "CezaAnketId");
                var storageName = DocumentStep.DeriveStorageName(PenaltySurveyLogoBlobs, legacyId);

                return new Document
                {
                    DocumentName = $"ceza-anketi-logo-{legacyId}",
                    StorageName = storageName,
                    StoragePath = DocumentStep.BuildStoragePath(storageName, tenantId),
                    SizeBytes = Long(reader, "Boyut") ?? 0,
                    IsActive = true,
                    CreationTime = DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> PenaltySurveysAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var cityMap = await context.IdMap.LoadAsync("Sehir_T", cancellationToken);
        var districtMap = await context.IdMap.LoadAsync("Ilce_T", cancellationToken);
        var neighbourhoodMap = await context.IdMap.LoadAsync("Mahalle_T", cancellationToken);
        var logoMap = await context.IdMap.LoadAsync(PenaltySurveyLogoBlobs, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<PenaltySurvey>(
            context, "CezaAnketi_T", "penalty surveys",
            """
            SELECT CezaAnketId, FirmaUnvani, TesisinAdi, TesisSorumlusu, TesisSorumlusununGorevi,
                   TesisSorumlusuGSM, IsverenAdiSoyadi, Telefon, Fax, EPosta, SehirId, IlceId,
                   MahalleId, Adres, FaturaAdresi, VergiDairesi, VergiNumarasi, CalisanSayisi,
                   SgkSicilNumarasi, TehlikeSinifi, KurumId, EklemeTarihi, GuncellemeTarihi
            FROM CezaAnketi_T ORDER BY CezaAnketId;
            """,
            "CezaAnketId",
            (reader, _) => new PenaltySurvey
            {
                CompanyTitle = Fit(context, "PenaltySurvey", "CompanyTitle", Text(reader, "FirmaUnvani")) ?? string.Empty,
                FacilityName = Fit(context, "PenaltySurvey", "FacilityName", Text(reader, "TesisinAdi")),
                FacilityOwner = Fit(context, "PenaltySurvey", "FacilityOwner", Text(reader, "TesisSorumlusu")),
                FacilityOwnerDuty = Fit(context, "PenaltySurvey", "FacilityOwnerDuty", Text(reader, "TesisSorumlusununGorevi")),
                FacilityOwnerGsm = Fit(context, "PenaltySurvey", "FacilityOwnerGsm", Text(reader, "TesisSorumlusuGSM")),
                EmployerNameLastName = Fit(context, "PenaltySurvey", "EmployerNameLastName", Text(reader, "IsverenAdiSoyadi")),
                Phone = Fit(context, "PenaltySurvey", "Phone", Text(reader, "Telefon")),
                Fax = Fit(context, "PenaltySurvey", "Fax", Text(reader, "Fax")),
                Email = Fit(context, "PenaltySurvey", "Email", Text(reader, "EPosta")),
                CityId = Lookup(cityMap, Int(reader, "SehirId")),
                DistrictId = Lookup(districtMap, Int(reader, "IlceId")),
                NeighborhoodId = Lookup(neighbourhoodMap, Int(reader, "MahalleId")),
                Address = Fit(context, "PenaltySurvey", "Address", Text(reader, "Adres")),
                InvoiceAddress = Fit(context, "PenaltySurvey", "InvoiceAddress", Text(reader, "FaturaAdresi")),
                TaxTaxOffice = Fit(context, "PenaltySurvey", "TaxTaxOffice", Text(reader, "VergiDairesi")),
                TaxNumber = Fit(context, "PenaltySurvey", "TaxNumber",
                    LegacyCrypt.TryDecrypt(Text(reader, "VergiNumarasi")) ?? Text(reader, "VergiNumarasi")),
                WorkerCount = Int(reader, "CalisanSayisi"),
                SsiRegistrationNumber = Fit(context, "PenaltySurvey", "SsiRegistrationNumber", Text(reader, "SgkSicilNumarasi")),
                HazardClass = HazardClassOf(Text(reader, "TehlikeSinifi")),
                LogoDocumentId = Lookup(logoMap, Required(reader, "CezaAnketId")),
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "GuncellemeTarihi"),
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken);
    }

    private static async Task<StepResult> PenaltySurveyLinesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var surveyMap = await context.IdMap.LoadAsync("CezaAnketi_T", cancellationToken);
        var penaltyMap = await context.IdMap.LoadAsync("Ceza_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<PenaltySurveyLine>(
            context, "CezaAnketiItem_T", "penalty survey lines",
            """
            SELECT CezaAnketiItemId, CezaAnketiId, CezaId, AnketCevabi, CezaTutari, Carpan,
                   CarpanHesapla, KurumId
            FROM CezaAnketiItem_T ORDER BY CezaAnketiItemId;
            """,
            "CezaAnketiItemId",
            (reader, orphan) =>
            {
                if (!surveyMap.TryGetValue(Required(reader, "CezaAnketiId"), out var surveyId)
                    || !penaltyMap.TryGetValue(Required(reader, "CezaId"), out var penaltyId))
                {
                    orphan();
                    return null;
                }

                return new PenaltySurveyLine
                {
                    PenaltySurveyId = surveyId,
                    PenaltyId = penaltyId,
                    SurveyAnswer = Bit(reader, "AnketCevabi"),
                    PenaltyAmount = Money(reader, "CezaTutari"),
                    Multiplier = Money(reader, "Carpan"),
                    MultiplierCalculate = Bit(reader, "CarpanHesapla"),
                    CreationTime = DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ contracts and prospects

    private static async Task<StepResult> ContractsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var salesRepMap = await context.IdMap.LoadAsync("Temsilci_T", cancellationToken);
        var (planIds, typeIds) = await LoadPlanAndTypeAsync(context, cancellationToken);

        return await CopyAsync<OrganizationContract>(
            context, "SozlesmeliFirmalar_T", "organization contracts",
            """
            SELECT Id, FirmaId, FirmaAdi, YetkiliTcNo, YetkiliAdi, YetkiliSoyadi, Email, Telefon,
                   Adres, SozlesmeTarihi, BirimFiyat, KullaniciSayisi, ToplamFiyat, Paket,
                   KurumTuru, Onay, Odendi, TemsilciId, ReferansId, AtamaLogu, Durum, [Not],
                   SozlesmeDurum, SozlesmeNotu, SozlesmeDurumTarihi, HesapKapanmaTarihi, EklenmeTarihi
            FROM SozlesmeliFirmalar_T ORDER BY Id;
            """,
            "Id",
            (reader, orphan) =>
            {
                if (Int(reader, "FirmaId") is not int legacyOrganizationId
                    || !organizationMap.TryGetValue(legacyOrganizationId, out var organizationId))
                {
                    orphan();
                    return null;
                }

                return new OrganizationContract
                {
                    OrganizationId = organizationId,
                    OrganizationName = Fit(context, "OrganizationContract", "OrganizationName", Text(reader, "FirmaAdi")) ?? string.Empty,

                    // Encrypted in the legacy row and encrypted again on the way in, through the
                    // destination's own converter; the plaintext exists only in between.
                    AuthorizedNationalId = Fit(context, "OrganizationContract", "AuthorizedNationalId",
                        LegacyCrypt.TryDecrypt(Text(reader, "YetkiliTcNo"))),

                    AuthorizedName = Fit(context, "OrganizationContract", "AuthorizedName", Text(reader, "YetkiliAdi")),
                    AuthorizedLastName = Fit(context, "OrganizationContract", "AuthorizedLastName", Text(reader, "YetkiliSoyadi")),
                    Email = Fit(context, "OrganizationContract", "Email", Text(reader, "Email")),
                    Phone = Fit(context, "OrganizationContract", "Phone", Text(reader, "Telefon")),
                    Address = Fit(context, "OrganizationContract", "Address", Text(reader, "Adres")),
                    ContractDate = Date(reader, "SozlesmeTarihi") ?? Date(reader, "EklenmeTarihi") ?? DateTime.Now,
                    UnitPrice = Money(reader, "BirimFiyat"),
                    UserCount = Int(reader, "KullaniciSayisi") ?? 0,
                    TotalPrice = Money(reader, "ToplamFiyat"),
                    SubscriptionPlanId = PlanOf(planIds, Text(reader, "Paket")),
                    OrganizationTypeId = TypeOf(typeIds, Text(reader, "KurumTuru")),
                    IsApproved = Bit(reader, "Onay"),
                    Paid = Bit(reader, "Odendi"),
                    SalesRepId = Lookup(salesRepMap, Int(reader, "TemsilciId")),
                    ReferenceCompanyId = null,
                    AssignmentLogId = Int(reader, "AtamaLogu"),
                    IsActive = Bit(reader, "Durum"),
                    Note = Fit(context, "OrganizationContract", "Note", Text(reader, "Not")),
                    ContractStatus = ContractStatusOf(Text(reader, "SozlesmeDurum")),
                    ContractNote = Fit(context, "OrganizationContract", "ContractNote", Text(reader, "SozlesmeNotu")),
                    ContractStatusDate = Date(reader, "SozlesmeDurumTarihi"),
                    AccountClosingDate = Date(reader, "HesapKapanmaTarihi"),
                    CreationTime = Date(reader, "EklenmeTarihi") ?? DateTime.Now,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>CustomerPackage_T</c> to <see cref="ProspectOrganization"/> — the sign-up funnel.
    /// <para>
    /// <b>The password is not carried.</b> The legacy table stores an eight-character plaintext
    /// password for each prospect so it can be mailed to them. It is a credential, the destination
    /// has nowhere to put it, and the right place for it is a reset link.
    /// </para>
    /// </summary>
    private static async Task<StepResult> ProspectsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var salesRepMap = await context.IdMap.LoadAsync("Temsilci_T", cancellationToken);
        var (planIds, _) = await LoadPlanAndTypeAsync(context, cancellationToken);

        var planByLegacyId = new Dictionary<int, string>
        {
            [1] = "startup",
            [2] = "pro",
            [3] = "demo",
            [4] = "ensa",
        };

        return await CopyAsync<ProspectOrganization>(
            context, "CustomerPackage_T", "prospects",
            """
            SELECT Id, Name, Surname, IsIndividual, NumberOfSpecialist, OSGBTitle, Phone, Email,
                   Address, IsDoctor, Price, PackageType, IsPaid, TCKN, IsDemo, IsMailSent,
                   KDV, KDVPrice, RegistrationDate, IsOSGB, FirmaId, TemsilciId, ReferansId,
                   AtamaLogu, Durum, [Not], SozlesmeDurum, SozlesmeNotu, SozlesmeDurumTarihi
            FROM CustomerPackage_T ORDER BY Id;
            """,
            "Id",
            (reader, _) => new ProspectOrganization
            {
                Name = Fit(context, "ProspectOrganization", "Name", Text(reader, "Name")) ?? string.Empty,
                LastName = Fit(context, "ProspectOrganization", "LastName", Text(reader, "Surname")) ?? string.Empty,
                NationalId = Fit(context, "ProspectOrganization", "NationalId",
                    LegacyCrypt.TryDecrypt(Text(reader, "TCKN")) ?? Text(reader, "TCKN")),
                OrganizationTitle = Fit(context, "ProspectOrganization", "OrganizationTitle", Text(reader, "OSGBTitle")),
                Phone = Fit(context, "ProspectOrganization", "Phone", Text(reader, "Phone")),
                Email = Fit(context, "ProspectOrganization", "Email", Text(reader, "Email")),
                Address = Fit(context, "ProspectOrganization", "Address", Text(reader, "Address")),
                IsIndividual = Bit(reader, "IsIndividual"),
                IsOhsProvider = Bit(reader, "IsOSGB"),
                PhysicianExists = Bit(reader, "IsDoctor"),
                SpecialistCount = Int(reader, "NumberOfSpecialist"),
                SubscriptionPlanId = Int(reader, "PackageType") is int legacyPlan
                                     && planByLegacyId.TryGetValue(legacyPlan, out var planCode)
                    ? PlanOf(planIds, planCode)
                    : null,
                Price = Money(reader, "Price"),
                VatRate = Money(reader, "KDV"),
                GrossWithVatPrice = Money(reader, "KDVPrice"),
                Paid = Bit(reader, "IsPaid"),
                IsDemo = Bit(reader, "IsDemo"),
                MailSent = Bit(reader, "IsMailSent"),
                RecordDate = Date(reader, "RegistrationDate"),
                OrganizationId = Lookup(organizationMap, Int(reader, "FirmaId")),
                SalesRepId = Lookup(salesRepMap, Int(reader, "TemsilciId")),
                ReferenceCompanyId = null,
                AssignmentLogId = Int(reader, "AtamaLogu"),
                IsActive = Bit(reader, "Durum"),
                Note = Fit(context, "ProspectOrganization", "Note", Text(reader, "Not")),
                ContractStatus = ContractStatusOf(Text(reader, "SozlesmeDurum")),
                ContractNote = Fit(context, "ProspectOrganization", "ContractNote", Text(reader, "SozlesmeNotu")),
                ContractStatusDate = Date(reader, "SozlesmeDurumTarihi"),
                CreationTime = Date(reader, "RegistrationDate") ?? DateTime.Now,
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ risk incident history

    /// <summary>
    /// The four legacy risk incident tables — work accident, no-damage accident, occupational
    /// disease, near miss — collapse into one, keyed by what each recorded.
    /// </summary>
    private static async Task<StepResult> RiskHistoryAsync(
        MigrationContext context,
        string legacyTable,
        string keyColumn,
        RiskHistoryRecordType type,
        CancellationToken cancellationToken)
    {
        var reportMap = await context.IdMap.LoadAsync("RiskAnalizRaporu_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<RiskAssessmentHistoryRecord>(
            context, legacyTable, $"risk history ({type})",
            $"SELECT {keyColumn}, RiskAnalizRaporuId, Tarih, Aciklama, KurumId FROM {legacyTable} ORDER BY {keyColumn};",
            keyColumn,
            (reader, orphan) =>
            {
                if (!reportMap.TryGetValue(Required(reader, "RiskAnalizRaporuId"), out var reportId))
                {
                    orphan();
                    return null;
                }

                return new RiskAssessmentHistoryRecord
                {
                    RiskAssessmentReportId = reportId,
                    RecordType = type,
                    Date = Date(reader, "Tarih") ?? DateTime.Now,
                    Description = Fit(context, "RiskAssessmentHistoryRecord", "Description", Text(reader, "Aciklama")) ?? string.Empty,
                    CreationTime = Date(reader, "Tarih") ?? DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ value mapping

    private static MessageType MessageTypeOf(int? type)
        => type switch
        {
            2 => MessageType.EmployeeSenderMessage,
            3 => MessageType.EmployeeRecipientMessage,
            4 => MessageType.SystemNotification,
            _ => MessageType.UserMessage,
        };

    /// <summary>
    /// <c>SozlesmeDurum</c>, a sales workflow stage, onto <see cref="ContractStatus"/>.
    /// <para>
    /// The legacy stages are finer than the destination's: "TeklifAtildi" and "MailAtildi" are both
    /// an offer having gone out, and "FaturaKesildi", "Fatura" and "KargoGeldi" are all a contract
    /// having been agreed. The record survives and the stage is coarsened, which is a real loss of
    /// granularity and is stated here rather than hidden — the exact stage is in the contract note
    /// beside it when anybody needs it.
    /// </para>
    /// </summary>
    private static ContractStatus ContractStatusOf(string? status)
        => Fold(status) switch
        {
            "TEKLIFATILDI" or "MAILATILDI" => ContractStatus.Sent,
            "FATURAKESILDI" or "FATURA" or "KARGOGELDI" => ContractStatus.Signed,
            "BOS" => ContractStatus.InPreparation,
            _ => ContractStatus.Unspecified,
        };

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

    private static int? PlanOf(Dictionary<string, int> planIds, string? legacyCode)
        => legacyCode is not null
           && SubscriptionPlanCodes.TryGetValue(legacyCode, out var code)
           && planIds.TryGetValue(code, out var id)
            ? id
            : null;

    private static int? TypeOf(Dictionary<string, int> typeIds, string? legacyCode)
        => legacyCode is not null
           && OrganizationTypeCodes.TryGetValue(legacyCode, out var code)
           && typeIds.TryGetValue(code, out var id)
            ? id
            : null;

    private static async Task<(Dictionary<string, int> Plans, Dictionary<string, int> Types)> LoadPlanAndTypeAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var plans = (await db.Set<SubscriptionPlan>().Select(p => new { p.Id, p.Code }).ToListAsync(cancellationToken))
            .ToDictionary(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

        var types = (await db.Set<OrganizationType>().Select(t => new { t.Id, t.Code }).ToListAsync(cancellationToken))
            .ToDictionary(t => t.Code, t => t.Id, StringComparer.OrdinalIgnoreCase);

        return (plans, types);
    }

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
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, TEntity Entity)>();

        if (existingKey is not null)
        {
            foreach (var row in await db.Set<TEntity>().ToListAsync(cancellationToken))
            {
                seen.Add(existingKey(row));
            }

            db.ChangeTracker.Clear();
        }

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
            note += $", {duplicates} DROPPED as a repeat";
        }

        return new StepResult(read, written, orphaned + duplicates, note);
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
            CommandTimeout = 900,
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

    private static decimal Money(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return 0m;
        }

        var value = Convert.ToDouble(reader.GetValue(index));

        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) >= 1e15)
        {
            return 0m;
        }

        return Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
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
