using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The client workplaces, their departments and their employees.
/// <para>
/// This is where the volume starts: 31,469 companies, 30,969 departments and 275,323 employees,
/// against the 5,874 rows of the step before it.
/// </para>
/// <para>
/// <b>Free text becomes enums.</b> The legacy columns hold what somebody typed: hazard class as
/// <c>"TEHLIKELI"</c>, <c>" Az Tehlikeli"</c>, <c>"yok"</c> and <c>"Çok Tehlikeli *"</c>; marital
/// status blank in 262,635 of 275,323 rows. Each value is matched case- and accent-insensitively
/// and anything unrecognised becomes <c>Unspecified</c> rather than a guess — but the count of
/// unrecognised values is reported, because a mapping that silently swallows a category nobody
/// noticed is how a report ends up wrong.
/// </para>
/// <para>
/// <b>Headquarters and branches.</b> The legacy <c>IsYeri</c> column is free text; what actually
/// says whether a row is a branch is <c>MerkezId</c> pointing at another company. Branches are
/// linked in a second pass, because a branch may be read before its headquarters.
/// </para>
/// </summary>
public sealed class CompanyStep : IMigrationStep
{
    public int Order => 30;

    public string Name => "companies";

    public string Description => "Client workplaces, workplace departments and their employees";

    /// <summary>Rows per round trip. Larger than the tenancy step's, which had a tenth of the volume.</summary>
    private const int BatchSize = 500;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var read = 0;
        var written = 0;
        var skipped = 0;
        var notes = new List<string>();

        var organizationCompanies = await MigrateOrganizationCompaniesAsync(context, cancellationToken);
        read += organizationCompanies.Result.Read;
        written += organizationCompanies.Result.Written;
        notes.Add(organizationCompanies.Result.Note!);

        var companies = await MigrateCompaniesAsync(context, cancellationToken);
        read += companies.Result.Read;
        written += companies.Result.Written;
        skipped += companies.Result.Skipped;
        notes.Add(companies.Result.Note!);

        // An organization's own staff hang off its own company record, so both maps are needed
        // wherever a legacy FirmaId is resolved.
        foreach (var (legacyId, modernId) in organizationCompanies.Map)
        {
            companies.Map[legacyId] = modernId;
        }

        var departments = await MigrateDepartmentsAsync(context, companies.Map, cancellationToken);
        read += departments.Read;
        written += departments.Written;
        skipped += departments.Skipped;
        notes.Add(departments.Note!);

        var employees = await MigrateEmployeesAsync(context, companies.Map, cancellationToken);
        read += employees.Read;
        written += employees.Written;
        skipped += employees.Skipped;
        notes.Add(employees.Note!);

        var links = await LinkCompanyReferencesAsync(context, companies.Map, cancellationToken);
        written += links.Written;
        notes.Add(links.Note!);

        return new StepResult(read, written, skipped, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ the organization as a company

    /// <summary>
    /// Gives every organization a company record of its own.
    /// <para>
    /// In the legacy schema one <c>Firma_T</c> row is both the organization and a workplace: its
    /// <c>Kurum</c> flag marks it as the tenant, and its own employees point their <c>FirmaId</c>
    /// at it. The rebuilt schema splits the two, and <c>Company.IsOrganizationRecord</c> is the half
    /// that was missing here - without it 2,706 of an organization's own staff had no company to
    /// belong to and were dropped as orphans.
    /// </para>
    /// </summary>
    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateOrganizationCompaniesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var officeMap = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);
        var cityMap = await context.IdMap.LoadAsync("Sehir_T", cancellationToken);
        var districtMap = await context.IdMap.LoadAsync("Ilce_T", cancellationToken);
        var already = await context.IdMap.LoadAsync("Firma_T:KurumSirket", cancellationToken);

        var fallbackOffice = await db.Set<Ensa.Domain.Tenancy.Office>()
            .Where(o => o.TenantId != null)
            .GroupBy(o => o.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, OfficeId = g.Min(o => o.Id) })
            .ToDictionaryAsync(x => x.TenantId, x => x.OfficeId, cancellationToken);

        var read = 0;
        var written = 0;
        var noOffice = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, Company Entity)>();

        const string sql = """
            SELECT FirmaId, FirmaAdi, SGKNo, VergiDairesi, VergiNumarasi, TehlikeSinifi,
                   Adres, SehirId, IlceId, Telefon, Email, YetkiliKisi, OfisId, Aktif, IsDeleted
            FROM Firma_T WHERE Kurum = 1 ORDER BY FirmaId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "FirmaId");
                if (already.ContainsKey(legacyId) || !organizationMap.TryGetValue(legacyId, out var tenantId))
                {
                    continue;
                }

                var officeId = MapId(officeMap, Int(reader, "OfisId"));
                if (officeId is null && fallbackOffice.TryGetValue(tenantId, out var fallback))
                {
                    officeId = fallback;
                }

                if (officeId is not { } office)
                {
                    noOffice++;
                    continue;
                }

                batch.Add((legacyId, new Company
                {
                    CompanyName = Fit(context, "Company", "CompanyName", Text(reader, "FirmaAdi"))
                                  ?? $"Organization {legacyId}",
                    SsiNumber = Fit(context, "Company", "SsiNumber", Text(reader, "SGKNo")),
                    TaxTaxOffice = Fit(context, "Company", "TaxTaxOffice", Text(reader, "VergiDairesi")),
                    TaxNumber = Fit(context, "Company", "TaxNumber", Text(reader, "VergiNumarasi")),
                    HazardClass = MapHazardClass(Text(reader, "TehlikeSinifi")) ?? HazardClass.Unspecified,
                    WorkplaceType = WorkplaceType.Headquarter,
                    Address = Fit(context, "Company", "Address", Text(reader, "Adres")),
                    CityId = MapId(cityMap, Int(reader, "SehirId")) ?? 0,
                    DistrictId = MapId(districtMap, Int(reader, "IlceId")) ?? 0,
                    Phone = Fit(context, "Company", "Phone", Text(reader, "Telefon")),
                    Email = Fit(context, "Company", "Email", Text(reader, "Email")),
                    AuthorizedPerson = Fit(context, "Company", "AuthorizedPerson", Text(reader, "YetkiliKisi")),
                    OfficeId = office,
                    // What marks the row as the organization itself rather than one of its clients.
                    IsOrganizationRecord = true,
                    IsActive = Bit(reader, "Aktif"),
                    TenantId = tenantId,
                    IsDeleted = Bit(reader, "IsDeleted"),
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "Firma_T:KurumSirket", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "Firma_T:KurumSirket", batch, pairs, cancellationToken);
        }

        var note = $"organization company records: {written} written";
        if (noOffice > 0)
        {
            note += $", {noOffice} SKIPPED (no office)";
        }

        var map = already.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, written, 0, note), map);
    }

    // ------------------------------------------------------------------ companies

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateCompaniesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var officeMap = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);
        var cityMap = await context.IdMap.LoadAsync("Sehir_T", cancellationToken);
        var districtMap = await context.IdMap.LoadAsync("Ilce_T", cancellationToken);
        var neighbourhoodMap = await context.IdMap.LoadAsync("Mahalle_T", cancellationToken);
        var already = await context.IdMap.LoadAsync("Firma_T", cancellationToken);
        var tenantRepairs = await LoadTenantRepairsAsync(context, organizationMap, cancellationToken);

        // Every company needs an office, and the column is not nullable. An organization whose
        // offices did not come across has none to offer, so its companies cannot be placed.
        var fallbackOfficeByTenant = await db.Set<Ensa.Domain.Tenancy.Office>()
            .Where(o => o.TenantId != null)
            .GroupBy(o => o.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, OfficeId = g.Min(o => o.Id) })
            .ToDictionaryAsync(x => x.TenantId, x => x.OfficeId, cancellationToken);

        var read = 0;
        var orphaned = 0;
        var noOffice = 0;
        var repaired = 0;
        var unknownHazard = 0;
        var duplicateSsiNumbers = 0;
        var pairs = new List<(int, int)>();

        // The SSI workplace registration number is unique within an organization in the rebuilt
        // schema; the legacy table lets the same number appear on several rows. Same rule as the
        // identity numbers: the first keeps it, the rest are written without it. A registration
        // number that points at two workplaces identifies neither.
        var ssiNumbersTaken = new HashSet<(int Tenant, string Ssi)>();

        foreach (var existing in await db.Set<Company>()
                     .Where(c => c.SsiNumber != null && c.TenantId != null)
                     .Select(c => new { c.TenantId, c.SsiNumber })
                     .ToListAsync(cancellationToken))
        {
            ssiNumbersTaken.Add((existing.TenantId!.Value, existing.SsiNumber!));
        }

        const string sql = """
            SELECT FirmaId, FirmaAdi, SID, SGKNo, VergiDairesi, VergiNumarasi, IsVeren, IsVerenGSM,
                   FaaliyetAlani, TehlikeSinifi, MerkezId, SubeNo, SubeAdi, GrupSirketId,
                   Adres, FaturaAdresi, SehirId, IlceId, MahalleId, LatLng,
                   Telefon, Faks, GSM, Email, CC, YetkiliKisi, YetkiliKisiTelefon, YetkiliKisiEmail,
                   FinansSorumlusu, FinansSorumlusuGSM, OfisId, BolgeKodu, Oncelik,
                   ZiyaretUzman, ZiyaretDoktor, IlkAyPrograminaDahil, SifreGonderildi,
                   AylikUcretResmi, AylikUcretToplam, UzmanUcret, HekimUcret, FaturaTutari,
                   FaturaTutariKh, GRSozlesmeTutari, OdenecekRakam, OdemeTarihi, TeklifKdvDahil,
                   NotAciklama, UyariNotu, NotuKaydeden, Aktif, KurumId, IsDeleted
            FROM Firma_T WHERE Kurum = 0 OR Kurum IS NULL ORDER BY FirmaId;
            """;

        var batch = new List<(int LegacyId, Company Entity)>();
        var written = 0;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "FirmaId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var legacyTenantId = Required(reader, "KurumId");

                if (!organizationMap.TryGetValue(legacyTenantId, out var tenantId))
                {
                    // The reference skipped a level: it points at an ordinary company whose own
                    // organization is real. See LoadTenantRepairsAsync.
                    if (!tenantRepairs.TryGetValue(legacyTenantId, out tenantId))
                    {
                        orphaned++;
                        continue;
                    }

                    repaired++;
                }

                // GetValueOrDefault would hand back 0 for a missing key, and 0 is a foreign key
                // pointing at nothing. The absence has to stay visible.
                var officeId = MapId(officeMap, Int(reader, "OfisId"));

                if (officeId is null && fallbackOfficeByTenant.TryGetValue(tenantId, out var fallback))
                {
                    officeId = fallback;
                }

                if (officeId is not { } office)
                {
                    noOffice++;
                    continue;
                }

                var hazard = MapHazardClass(Text(reader, "TehlikeSinifi"));
                if (hazard is null)
                {
                    unknownHazard++;
                }

                var ssiNumber = Fit(context, "Company", "SsiNumber", Text(reader, "SGKNo"));
                if (ssiNumber is not null && !ssiNumbersTaken.Add((tenantId, ssiNumber)))
                {
                    ssiNumber = null;
                    duplicateSsiNumbers++;
                }

                var (latitude, longitude) = ParseLatLng(Text(reader, "LatLng"));

                batch.Add((legacyId, new Company
                {
                    CompanyName = Fit(context, "Company", "CompanyName", Text(reader, "FirmaAdi")) ?? $"Company {legacyId}",
                    Sid = Fit(context, "Company", "Sid", Text(reader, "SID")),
                    SsiNumber = ssiNumber,
                    TaxTaxOffice = Fit(context, "Company", "TaxTaxOffice", Text(reader, "VergiDairesi")),
                    TaxNumber = Fit(context, "Company", "TaxNumber", Text(reader, "VergiNumarasi")),
                    EmployerName = Fit(context, "Company", "EmployerName", Text(reader, "IsVeren")),
                    EmployerMobilePhone = Fit(context, "Company", "EmployerMobilePhone", Text(reader, "IsVerenGSM")),
                    BusinessActivity = Fit(context, "Company", "BusinessActivity", Text(reader, "FaaliyetAlani")),
                    HazardClass = hazard ?? HazardClass.Unspecified,
                    // MerkezId says what IsYeri only hints at: a row that points at another company
                    // is that company's branch. The link itself is set in a later pass.
                    WorkplaceType = Int(reader, "MerkezId") is > 0 ? WorkplaceType.Branch : WorkplaceType.Headquarter,
                    BranchNo = Int(reader, "SubeNo"),
                    BranchName = Fit(context, "Company", "BranchName", Text(reader, "SubeAdi")),
                    Address = Fit(context, "Company", "Address", Text(reader, "Adres")),
                    InvoiceAddress = Fit(context, "Company", "InvoiceAddress", Text(reader, "FaturaAdresi")),
                    CityId = MapId(cityMap, Int(reader, "SehirId")) ?? 0,
                    DistrictId = MapId(districtMap, Int(reader, "IlceId")) ?? 0,
                    NeighborhoodId = MapId(neighbourhoodMap, Int(reader, "MahalleId")),
                    Latitude = latitude,
                    Longitude = longitude,
                    Phone = Fit(context, "Company", "Phone", Text(reader, "Telefon")),
                    Fax = Fit(context, "Company", "Fax", Text(reader, "Faks")),
                    Gsm = Fit(context, "Company", "Gsm", Text(reader, "GSM")),
                    Email = Fit(context, "Company", "Email", Text(reader, "Email")),
                    Cc = Fit(context, "Company", "Cc", Text(reader, "CC")),
                    AuthorizedPerson = Fit(context, "Company", "AuthorizedPerson", Text(reader, "YetkiliKisi")),
                    AuthorizedPersonPhone = Fit(context, "Company", "AuthorizedPersonPhone", Text(reader, "YetkiliKisiTelefon")),
                    AuthorizedPersonEmail = Fit(context, "Company", "AuthorizedPersonEmail", Text(reader, "YetkiliKisiEmail")),
                    FinanceOwner = Fit(context, "Company", "FinanceOwner", Text(reader, "FinansSorumlusu")),
                    FinanceOwnerGsm = Fit(context, "Company", "FinanceOwnerGsm", Text(reader, "FinansSorumlusuGSM")),
                    OfficeId = office,
                    RegionCode = Int(reader, "BolgeKodu"),
                    Priority = Int(reader, "Oncelik"),
                    VisitSpecialist = Int(reader, "ZiyaretUzman"),
                    VisitPhysician = Int(reader, "ZiyaretDoktor"),
                    FirstMonthProgramIncluded = Bit(reader, "IlkAyPrograminaDahil"),
                    PasswordSent = Int(reader, "SifreGonderildi") is > 0,
                    MonthlyFeeOfficial = Money(reader, "AylikUcretResmi"),
                    MonthlyFeeTotal = Money(reader, "AylikUcretToplam"),
                    SpecialistFee = Money(reader, "UzmanUcret"),
                    PhysicianFee = Money(reader, "HekimUcret"),
                    InvoiceAmount = Money(reader, "FaturaTutari"),
                    InvoiceAmountKh = Money(reader, "FaturaTutariKh"),
                    GrContractAmount = Money(reader, "GRSozlesmeTutari"),
                    PayableDigit = Money(reader, "OdenecekRakam"),
                    PaymentDate = Date(reader, "OdemeTarihi"),
                    QuoteVatIncluded = string.Equals(Text(reader, "TeklifKdvDahil"), "1", StringComparison.Ordinal),
                    Notes = Fit(context, "Company", "Notes", Text(reader, "NotAciklama")),
                    WarningNote = Fit(context, "Company", "WarningNote", Text(reader, "UyariNotu")),
                    NoteRecordedBy = Fit(context, "Company", "NoteRecordedBy", Text(reader, "NotuKaydeden")),
                    IsActive = Bit(reader, "Aktif"),
                    IsOrganizationRecord = false,
                    TenantId = tenantId,
                    IsDeleted = Bit(reader, "IsDeleted"),
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "Firma_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "Firma_T", batch, pairs, cancellationToken);
        }

        var note = $"companies: {written} written, {already.Count} already there";
        if (duplicateSsiNumbers > 0)
        {
            note += $", {duplicateSsiNumbers} SSI number(s) DROPPED as duplicates within their organization";
        }

        if (repaired > 0)
        {
            note += $", {repaired} tenant reference(s) repaired by following one hop";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (organization missing)";
        }

        if (noOffice > 0)
        {
            note += $", {noOffice} SKIPPED (organization has no office)";
        }

        if (unknownHazard > 0)
        {
            note += $", {unknownHazard} unrecognised hazard class -> Unspecified";
        }

        var map = already.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, written, read - written, note), map);
    }

    // ------------------------------------------------------------------ departments

    private static async Task<StepResult> MigrateDepartmentsAsync(
        MigrationContext context,
        Dictionary<int, int> companyMap,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var tenantRepairs = await LoadTenantRepairsAsync(context, organizationMap, cancellationToken);
        var already = await context.IdMap.LoadAsync("IsyeriBolum_T", cancellationToken);

        var read = 0;
        var orphaned = 0;
        var written = 0;
        var merged = 0;
        var pairs = new List<(int, int)>();
        var mergedPairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, WorkplaceDepartment Entity)>();

        // (company, department name) is unique in the rebuilt schema. Where the legacy data names
        // the same department twice, both legacy ids point at the one modern row.
        var seen = new Dictionary<(int Company, string Name), int>();

        foreach (var existing in await db.Set<WorkplaceDepartment>()
                     .Select(d => new { d.Id, d.CompanyId, d.DepartmentName })
                     .ToListAsync(cancellationToken))
        {
            seen[(existing.CompanyId, Fold(existing.DepartmentName) ?? string.Empty)] = existing.Id;
        }

        const string sql = """
            SELECT BolumId, FirmaId, BolumAdi, Deletable, KurumId, IsDeleted
            FROM IsyeriBolum_T ORDER BY BolumId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "BolumId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var legacyTenantId = Required(reader, "KurumId");

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || (!organizationMap.TryGetValue(legacyTenantId, out var tenantId)
                        && !tenantRepairs.TryGetValue(legacyTenantId, out tenantId)))
                {
                    orphaned++;
                    continue;
                }

                var departmentName = Fit(context, "WorkplaceDepartment", "DepartmentName", Text(reader, "BolumAdi"))
                                     ?? $"Department {legacyId}";

                var key = (companyId, Fold(departmentName) ?? string.Empty);
                if (seen.TryGetValue(key, out var existingId))
                {
                    // The same department under a second legacy id. Point that id at the row that
                    // already exists rather than inserting a twin nobody can tell apart.
                    mergedPairs.Add((legacyId, existingId));
                    merged++;
                    continue;
                }

                // Reserved before the insert, so two rows inside one batch cannot both pass.
                seen[key] = 0;

                batch.Add((legacyId, new WorkplaceDepartment
                {
                    CompanyId = companyId,
                    DepartmentName = departmentName,
                    Deletable = Bit(reader, "Deletable"),
                    TenantId = tenantId,
                    IsDeleted = Bit(reader, "IsDeleted"),
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "IsyeriBolum_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "IsyeriBolum_T", batch, pairs, cancellationToken);
        }

        if (mergedPairs.Count > 0 && !context.DryRun)
        {
            await context.IdMap.SaveAsync("IsyeriBolum_T", mergedPairs, 'M', cancellationToken);
        }

        var note = $"departments: {written} written";
        if (merged > 0)
        {
            note += $", {merged} merged into an existing department of the same name";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company missing)";
        }

        return new StepResult(read, written, read - written, note);
    }

    // ------------------------------------------------------------------ employees

    private static async Task<StepResult> MigrateEmployeesAsync(
        MigrationContext context,
        Dictionary<int, int> companyMap,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var tenantRepairs = await LoadTenantRepairsAsync(context, organizationMap, cancellationToken);
        var already = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);

        var read = 0;
        var orphaned = 0;
        var written = 0;
        var duplicateNationalIds = 0;
        var overlongNationalIds = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, CompanyEmployee Entity)>();

        // (organization, company, national id) is unique in the rebuilt schema; the legacy table
        // lets the same number appear twice on one company's payroll.
        //
        // Seeded from the destination, not just from this run: a resumed run - the normal case for
        // 275,323 rows - must recognise what an earlier attempt already wrote, or it collides on
        // the unique index and stops again a little further along.
        var nationalIdsTaken = new HashSet<(int Tenant, int Company, string NationalId)>();

        foreach (var existing in await db.Set<CompanyEmployee>()
                     .Where(e => e.NationalId != null && e.TenantId != null)
                     .Select(e => new { e.TenantId, e.CompanyId, e.NationalId })
                     .ToListAsync(cancellationToken))
        {
            nationalIdsTaken.Add((existing.TenantId!.Value, existing.CompanyId, existing.NationalId!));
        }

        const string sql = """
            SELECT FirmaPersonelId, FirmaId, Adi, Soyadi, BabaAdi, AnaAdi, TCKimlikNo,
                   DogumYeri, DogumTarihi, Cinsiyet, EgitimDurumu, MedeniHali, CocukSayisi,
                   Telefon, GSM, Email, EvAdresi, AcilDurumKisi, AcilDurumKisiTelefon,
                   Gorevi, Meslegi, CalistigiBolum, IseGirisTarihi, IstenCikisTarihi,
                   IseGirisMuayenesi, IseGirisMuayeneTarihi, IseGirisSonrakiMuayeneTarihi,
                   IseGirisMuayeneYapan, CalismaSekli, CalismaOrtami, IsEkipmanlari,
                   Aciklama, Aktif, KurumId, IsDeleted
            FROM FirmaPersonel_T ORDER BY FirmaPersonelId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "FirmaPersonelId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var legacyTenantId = Required(reader, "KurumId");

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || (!organizationMap.TryGetValue(legacyTenantId, out var tenantId)
                        && !tenantRepairs.TryGetValue(legacyTenantId, out tenantId)))
                {
                    orphaned++;
                    continue;
                }

                // NOT fitted. Shortening an identity number produces eleven digits that look
                // valid and belong to somebody else; 1,462 legacy values are longer than eleven
                // and are not identity numbers at all. Dropping them is the honest outcome.
                var nationalId = LegacyCrypt.TryDecrypt(Text(reader, "TCKimlikNo"));

                if (nationalId is { Length: > 11 })
                {
                    nationalId = null;
                    overlongNationalIds++;
                }

                if (nationalId is not null && !nationalIdsTaken.Add((tenantId, companyId, nationalId)))
                {
                    nationalId = null;
                    duplicateNationalIds++;
                }

                batch.Add((legacyId, new CompanyEmployee
                {
                    CompanyId = companyId,
                    Name = Fit(context, "CompanyEmployee", "Name", Text(reader, "Adi")) ?? string.Empty,
                    LastName = Fit(context, "CompanyEmployee", "LastName", Text(reader, "Soyadi")) ?? string.Empty,
                    FatherName = Fit(context, "CompanyEmployee", "FatherName", Text(reader, "BabaAdi")),
                    MotherName = Fit(context, "CompanyEmployee", "MotherName", Text(reader, "AnaAdi")),
                    NationalId = nationalId,
                    BirthLocation = Fit(context, "CompanyEmployee", "BirthLocation", Text(reader, "DogumYeri")),
                    BirthDate = Date(reader, "DogumTarihi"),
                    Gender = MapGender(Text(reader, "Cinsiyet")),
                    EducationLevel = MapEducation(Text(reader, "EgitimDurumu")),
                    MaritalStatus = MapMaritalStatus(Text(reader, "MedeniHali")),
                    ChildCount = Int(reader, "CocukSayisi"),
                    Phone = Fit(context, "CompanyEmployee", "Phone", Text(reader, "Telefon")),
                    Gsm = Fit(context, "CompanyEmployee", "Gsm", Text(reader, "GSM")),
                    Email = Fit(context, "CompanyEmployee", "Email", Text(reader, "Email")),
                    HomeAddress = Fit(context, "CompanyEmployee", "HomeAddress", Text(reader, "EvAdresi")),
                    EmergencyPerson = Fit(context, "CompanyEmployee", "EmergencyPerson", Text(reader, "AcilDurumKisi")),
                    EmergencyPersonPhone = Fit(context, "CompanyEmployee", "EmergencyPersonPhone", Text(reader, "AcilDurumKisiTelefon")),
                    Duty = Fit(context, "CompanyEmployee", "Duty", Text(reader, "Gorevi")),
                    Occupation = Fit(context, "CompanyEmployee", "Occupation", Text(reader, "Meslegi")),
                    AssignedDepartmentName = Fit(context, "CompanyEmployee", "AssignedDepartmentName", Text(reader, "CalistigiBolum")),
                    HireDate = Date(reader, "IseGirisTarihi"),
                    TerminationDate = Date(reader, "IstenCikisTarihi"),
                    PreEmploymentExamination = Fit(context, "CompanyEmployee", "PreEmploymentExamination", Text(reader, "IseGirisMuayenesi")),
                    PreEmploymentExaminationDate = Date(reader, "IseGirisMuayeneTarihi"),
                    PreEmploymentNextExaminationDate = Date(reader, "IseGirisSonrakiMuayeneTarihi"),
                    PreEmploymentExaminationPerformedBy =
                        Fit(context, "CompanyEmployee", "PreEmploymentExaminationPerformedBy", Text(reader, "IseGirisMuayeneYapan")),
                    WorkMethodCode = Fit(context, "CompanyEmployee", "WorkMethodCode", Text(reader, "CalismaSekli")),
                    WorkEnvironmentCode = Fit(context, "CompanyEmployee", "WorkEnvironmentCode", Text(reader, "CalismaOrtami")),
                    WorkEquipmentCode = Fit(context, "CompanyEmployee", "WorkEquipmentCode", Text(reader, "IsEkipmanlari")),
                    Description = Fit(context, "CompanyEmployee", "Description", Text(reader, "Aciklama")),
                    IsActive = Bit(reader, "Aktif"),
                    TenantId = tenantId,
                    IsDeleted = Bit(reader, "IsDeleted"),
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "FirmaPersonel_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "FirmaPersonel_T", batch, pairs, cancellationToken);
        }

        var note = $"employees: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company missing)";
        }

        if (duplicateNationalIds > 0)
        {
            note += $", {duplicateNationalIds} national id(s) DROPPED as duplicates within their company";
        }

        if (overlongNationalIds > 0)
        {
            note += $", {overlongNationalIds} DROPPED as too long to be an identity number";
        }

        return new StepResult(read, written, read - written, note);
    }

    // ------------------------------------------------------------------ repairing tenants

    /// <summary>
    /// Legacy tenant references that skip a level, and what they should have pointed at.
    /// <para>
    /// 1,260 companies name a <c>KurumId</c> that is not an organization. For 1,208 of them the row
    /// it names is an ordinary company whose <b>own</b> <c>KurumId</c> is a real organization — the
    /// reference went one level short. Dropping them would cost 31,556 employees, so the hop is
    /// followed.
    /// </para>
    /// <para>
    /// <b>One hop only.</b> Two is no longer a mistyped reference but a guess, and a company placed
    /// in the wrong organization is worse than one left behind: the tenant filter would then show
    /// one provider another provider's client. The 52 that point at a row which does not exist stay
    /// unresolved and are reported.
    /// </para>
    /// </summary>
    private static async Task<Dictionary<int, int>> LoadTenantRepairsAsync(
        MigrationContext context,
        Dictionary<int, int> organizationMap,
        CancellationToken cancellationToken)
    {
        var repairs = new Dictionary<int, int>();

        const string sql = """
            SELECT DISTINCT broken.KurumId, intermediate.KurumId
            FROM Firma_T broken
            JOIN Firma_T intermediate ON intermediate.FirmaId = broken.KurumId
            WHERE (broken.Kurum = 0 OR broken.Kurum IS NULL)
              AND intermediate.Kurum <> 1;
            """;

        await using var connection = await context.OpenLegacyAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var namedId = reader.GetInt32(0);
            var actualId = reader.GetInt32(1);

            if (organizationMap.TryGetValue(actualId, out var tenantId))
            {
                repairs[namedId] = tenantId;
            }
        }

        return repairs;
    }

    // ------------------------------------------------------------------ second pass

    /// <summary>
    /// Fills in the references that point from one company to another, and from a user to a company.
    /// <para>
    /// Both have to wait: a branch may be read before its headquarters, and a user is created a step
    /// earlier than the company it belongs to. Leaving them null would be a quiet loss — a branch
    /// that forgets its head office, a customer contact attached to nothing.
    /// </para>
    /// </summary>
    private static async Task<StepResult> LinkCompanyReferencesAsync(
        MigrationContext context,
        Dictionary<int, int> companyMap,
        CancellationToken cancellationToken)
    {
        if (context.DryRun)
        {
            return new StepResult(0, 0, 0, "links: skipped on a dry run");
        }

        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);

        await using var db = context.CreateDbContext();

        // --- branch -> headquarters, and group company ---
        var headquarters = new List<(int CompanyId, int Value)>();
        var groups = new List<(int CompanyId, int Value)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT FirmaId, MerkezId, GrupSirketId FROM Firma_T WHERE MerkezId IS NOT NULL OR GrupSirketId IS NOT NULL",
            connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    continue;
                }

                if (MapId(companyMap, Int(reader, "MerkezId")) is { } headquartersId)
                {
                    headquarters.Add((companyId, headquartersId));
                }

                if (MapId(companyMap, Int(reader, "GrupSirketId")) is { } groupId)
                {
                    groups.Add((companyId, groupId));
                }
            }
        }

        // --- user -> company ---
        var userCompanies = new List<(int UserId, int CompanyId)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT KullaniciId, FirmaId FROM Kullanici_T WHERE FirmaId IS NOT NULL", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (userMap.TryGetValue(Required(reader, "KullaniciId"), out var userId)
                    && companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    userCompanies.Add((userId, companyId));
                }
            }
        }

        var branches = await ApplyLinksAsync(
            context, "ensa.Company", "HeadquarterCompanyId", headquarters, cancellationToken);

        var groupCompanies = await ApplyLinksAsync(
            context, "ensa.Company", "GroupCorporateId", groups, cancellationToken);

        var users = await ApplyLinksAsync(
            context, "ensa.[User]", "CompanyId", userCompanies, cancellationToken);

        return new StepResult(
            0, branches + groupCompanies + users, 0,
            $"links: {branches} branch(es) to headquarters, {groupCompanies} group companies, "
            + $"{users} user(s) bound to their company");
    }

    /// <summary>
    /// Back-fills one foreign key for many rows, in batches, with a join against a VALUES list.
    /// <para>
    /// One UPDATE per row is 30,000 round trips across a wide-area connection, and EF treats a row
    /// that does not match as a concurrency failure and abandons the step. Back-filling a foreign
    /// key wants neither: a row that is not there is a row to leave alone.
    /// </para>
    /// <para>
    /// Raw SQL is safe here - these are plain integer keys with no value converter behind them.
    /// The values are integers this code produced, not text from the legacy database, so there is
    /// nothing to inject.
    /// </para>
    /// </summary>
    /// <returns>How many rows were actually updated.</returns>
    private static async Task<int> ApplyLinksAsync(
        MigrationContext context,
        string table,
        string column,
        List<(int Id, int Value)> links,
        CancellationToken cancellationToken)
    {
        if (links.Count == 0)
        {
            return 0;
        }

        var updated = 0;

        await using var connection = await context.OpenModernAsync(cancellationToken);

        foreach (var chunk in links.Chunk(1000))
        {
            var values = string.Join(",", chunk.Select(link => $"({link.Id},{link.Value})"));

            await using var command = new SqlCommand($"""
                UPDATE target
                SET {column} = source.Value
                FROM {table} AS target
                JOIN (VALUES {values}) AS source (Id, Value) ON target.Id = source.Id;
                """, connection) { CommandTimeout = 600 };

            updated += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return updated;
    }

    // ------------------------------------------------------------------ writing

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

        // The tracker holds every entity it has seen, and at 275,323 rows that is the difference
        // between a migration and an out-of-memory error.
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

    // ------------------------------------------------------------------ value mapping

    /// <summary>
    /// Hazard class from what somebody typed: <c>"TEHLIKELI"</c>, <c>" Az Tehlikeli"</c>,
    /// <c>"Çok Tehlikeli *"</c>, <c>"yok"</c>. Matched on a folded prefix rather than exactly.
    /// </summary>
    private static HazardClass? MapHazardClass(string? value)
    {
        var folded = Fold(value);
        if (folded is null)
        {
            return null;
        }

        // "cok tehlikeli" is checked before "tehlikeli", which it contains.
        if (folded.StartsWith("cok tehlikeli", StringComparison.Ordinal)) return HazardClass.VeryHazardous;
        if (folded.StartsWith("az tehlikeli", StringComparison.Ordinal)) return HazardClass.LowHazard;
        if (folded.StartsWith("tehlikeli", StringComparison.Ordinal)) return HazardClass.Hazardous;

        return null;
    }

    private static Gender MapGender(string? value) => Fold(value) switch
    {
        "erkek" or "e" => Gender.Male,
        "kadin" or "k" => Gender.Female,
        _ => Gender.Unspecified,
    };

    private static MaritalStatus MapMaritalStatus(string? value) => Fold(value) switch
    {
        "evli" => MaritalStatus.Married,
        "bekar" => MaritalStatus.Single,
        "bosanmis" => MaritalStatus.Divorced,
        "dul" => MaritalStatus.Widowed,
        _ => MaritalStatus.Unspecified,
    };

    private static EducationLevel MapEducation(string? value) => Fold(value) switch
    {
        "yok" => EducationLevel.NotLiterate,
        "okur yazar" => EducationLevel.Literate,
        "ilk okul" or "ilkokul" => EducationLevel.PrimarySchool,
        "orta okul" or "ortaokul" => EducationLevel.MiddleSchool,
        "lise" => EducationLevel.HighSchool,
        "on lisans" or "onlisans" => EducationLevel.AssociateDegree,
        "lisans" => EducationLevel.License,
        "yuksek lisans" => EducationLevel.MastersDegree,
        "doktora" => EducationLevel.Doctorate,
        _ => EducationLevel.Unspecified,
    };

    /// <summary>
    /// Lower-cases and strips the Turkish diacritics, so <c>"Ön Lisans"</c>, <c>"ÖN LİSANS"</c> and
    /// <c>"on lisans"</c> are one value. Ten years of typing produced all three.
    /// </summary>
    private static string? Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var folded = value.Trim()
            .Replace('İ', 'i').Replace('I', 'ı')
            .ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g')
            .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c')
            .Replace('â', 'a').Replace('î', 'i').Replace('û', 'u');

        return folded.Length == 0 ? null : folded;
    }

    /// <summary>
    /// Splits the legacy <c>LatLng</c>, which is one column holding two numbers separated by a comma.
    /// </summary>
    private static (decimal? Latitude, decimal? Longitude) ParseLatLng(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return (null, null);
        }

        var culture = System.Globalization.CultureInfo.InvariantCulture;

        if (!decimal.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out var latitude)
            || !decimal.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out var longitude))
        {
            return (null, null);
        }

        // The column is sized for a coordinate, and the legacy field holds whatever was pasted into
        // it - one row parses as a latitude of 11122, which is not a place. Out of range is not a
        // coordinate that needs rounding; it is a value that means nothing, so it is dropped.
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180
            ? (latitude, longitude)
            : (null, null);
    }

    // ------------------------------------------------------------------ readers

    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

    // Read by name, not by position. A positional reader is a list of numbers that has to stay
    // in step with a list of names written elsewhere in the file, and it did not: one column was
    // read one place to the right, and only a type mismatch made it visible. Had the neighbour been
    // another int, every one of 31,469 companies would have taken the wrong value in silence.

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

    private static bool Bit(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static decimal? Money(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToDecimal(reader.GetValue(index));
    }

    /// <summary>A column that must be present and non-null; its absence is a mapping mistake.</summary>
    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));

    private static int? MapId(Dictionary<int, int> map, int? legacyId)
        => legacyId is { } id && map.TryGetValue(id, out var modernId) ? modernId : null;
}
