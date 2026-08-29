using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The organizations, their offices and their staff.
/// <para>
/// <b>Where the tenants come from.</b> The legacy schema has no organization table. A row in
/// <c>Firma_T</c> with <c>Kurum = 1</c> <i>is</i> the organization, and every other row's
/// <c>KurumId</c> points at it — 1,040 organizations among 32,509 companies. The rebuilt schema
/// splits that into <c>Organization</c> (the tenant) and <c>Company</c> (the workplace), which is
/// the same idea written down properly, so this step creates the tenants and the companies step
/// creates the rest.
/// </para>
/// <para>
/// <b>Passwords are not migrated, and cannot be.</b> The legacy <c>Sifre</c> column holds 128
/// characters of non-hexadecimal text, and different users share a prefix: that is reversible
/// encryption, not a hash. ASP.NET Identity stores PBKDF2 hashes, so there is nothing to convert —
/// and converting would be the wrong thing anyway. Credentials kept in a form somebody can decrypt
/// should be treated as already exposed; carrying them into a new system carries the exposure with
/// them. Every migrated user therefore arrives with an unusable random password and
/// <c>MustChangePassword</c> set, and regains access through a reset.
/// <b>This has an operational consequence: no migrated user can sign in until their password is
/// reset.</b>
/// </para>
/// <para>
/// <b>Usernames have to be invented.</b> <c>Kullanici_T</c> has no username column — the legacy
/// application signed people in by e-mail. But 600 users have no e-mail at all and 208 addresses
/// are shared by more than one account, while Identity needs a unique one each. The rule is
/// spelled out in <see cref="ResolveUserName"/>.
/// </para>
/// </summary>
public sealed class TenancyStep : IMigrationStep
{
    public int Order => 20;

    public string Name => "tenancy";

    public string Description => "Organizations, offices, users and their office assignments";

    /// <summary>
    /// Legacy <c>KurumTuru</c> to the seeded <c>OrganizationType</c> code.
    /// <para>
    /// <c>Kurumsal</c> becomes <c>ISGB</c>: a corporate customer running its own in-house unit is
    /// what an İSGB is. <c>ensa</c> is the vendor's own single record and is filed under
    /// <c>OSGB</c>. Both are assumptions, stated here rather than buried.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> OrganizationTypeCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["OSGB"] = "OSGB",
            ["Bireysel"] = "BIREYSEL",
            ["Kurumsal"] = "ISGB",
            ["ensa"] = "OSGB",
        };

    /// <summary>
    /// Legacy <c>PaketTuru</c> to the seeded <c>SubscriptionPlan</c> code.
    /// <para>
    /// The legacy <c>ensa</c> plan is the vendor's own unrestricted access and has no counterpart;
    /// it maps to the widest plan that does exist.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> SubscriptionPlanCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pro"] = "PROFESYONEL",
            ["demo"] = "DEMO",
            ["startup"] = "BASLANGIC",
            ["ensa"] = "KURUMSAL",
        };

    /// <summary>Legacy <c>PersonelTuru</c> to <see cref="StaffRole"/>.</summary>
    private static readonly Dictionary<string, StaffRole> StaffRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Uzman"] = StaffRole.OccupationalSafetySpecialist,
            ["Doktor"] = StaffRole.WorkplacePhysician,
            ["Diger Saglik"] = StaffRole.OtherHealthPersonnel,
            ["Diğer Sağlık"] = StaffRole.OtherHealthPersonnel,
            ["Ofis personeli"] = StaffRole.OfficeStaff,
            ["Musteri"] = StaffRole.Customer,
            ["Müşteri"] = StaffRole.Customer,
            ["ofis-admin"] = StaffRole.OfficeAdministrator,
            ["Admin"] = StaffRole.OrganizationAdministrator,
            ["ser-admin"] = StaffRole.SystemAdministrator,
        };

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var read = 0;
        var written = 0;
        var skipped = 0;
        var notes = new List<string>();

        var organizations = await MigrateOrganizationsAsync(context, cancellationToken);
        read += organizations.Result.Read;
        written += organizations.Result.Written;
        skipped += organizations.Result.Skipped;
        notes.Add(organizations.Result.Note!);

        var offices = await MigrateOfficesAsync(context, organizations.Map, cancellationToken);
        read += offices.Result.Read;
        written += offices.Result.Written;
        skipped += offices.Result.Skipped;
        notes.Add(offices.Result.Note!);

        var users = await MigrateUsersAsync(context, organizations.Map, offices.Map, cancellationToken);
        read += users.Read;
        written += users.Written;
        skipped += users.Skipped;
        notes.Add(users.Note!);

        return new StepResult(read, written, skipped, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ organizations

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateOrganizationsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var typeIds = await db.Set<OrganizationType>()
            .ToDictionaryAsync(t => t.Code, t => t.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var planIds = await db.Set<SubscriptionPlan>()
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var cityMap = await context.IdMap.LoadAsync("Sehir_T", cancellationToken);
        var districtMap = await context.IdMap.LoadAsync("Ilce_T", cancellationToken);
        var already = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        var defaultTypeId = typeIds["OSGB"];
        var defaultPlanId = planIds["DEMO"];

        var pairs = new List<(int, int)>();
        var read = 0;
        var unknownType = 0;
        var unknownPlan = 0;

        const string sql = """
            SELECT FirmaId, FirmaAdi, KurumTuru, PaketTuru, VergiDairesi, VergiNumarasi,
                   Adres, SehirId, IlceId, Telefon, Email, YetkiliKisi, YetkiliKisiTelefon,
                   YetkiliKisiEmail, Aktif, EklemeTarihi, IsDeleted
            FROM Firma_T WHERE Kurum = 1 ORDER BY FirmaId;
            """;

        var batch = new List<(int LegacyId, Organization Entity)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = reader.GetInt32(0);
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var typeCode = Text(reader, 2);
                var planCode = Text(reader, 3);

                if (typeCode is null || !OrganizationTypeCodes.TryGetValue(typeCode, out var mappedType))
                {
                    unknownType++;
                    mappedType = null;
                }

                if (planCode is null || !SubscriptionPlanCodes.TryGetValue(planCode, out var mappedPlan))
                {
                    unknownPlan++;
                    mappedPlan = null;
                }

                var organization = new Organization
                {
                    Name = Fit(context, "Organization", "Name", Text(reader, 1)) ?? $"Organization {legacyId}",
                    // The legacy schema has no organization code; the identity it was known by is
                    // its Firma_T id, so that is what the code preserves.
                    Code = $"L{legacyId}",
                    OrganizationTypeId = mappedType is null ? defaultTypeId : typeIds[mappedType],
                    SubscriptionPlanId = mappedPlan is null ? defaultPlanId : planIds[mappedPlan],
                    TaxOffice = Fit(context, "Organization", "TaxOffice", Text(reader, 4)),
                    TaxNumber = Fit(context, "Organization", "TaxNumber", Text(reader, 5)),
                    Address = Fit(context, "Organization", "Address", Text(reader, 6)),
                    CityId = MapId(cityMap, Int(reader, 7)),
                    DistrictId = MapId(districtMap, Int(reader, 8)),
                    Phone = Fit(context, "Organization", "Phone", Text(reader, 9)),
                    Email = Fit(context, "Organization", "Email", Text(reader, 10)),
                    AuthorizedFullName = Fit(context, "Organization", "AuthorizedFullName", Text(reader, 11)),
                    AuthorizedPhone = Fit(context, "Organization", "AuthorizedPhone", Text(reader, 12)),
                    AuthorizedEmail = Fit(context, "Organization", "AuthorizedEmail", Text(reader, 13)),
                    IsActive = !reader.IsDBNull(14) && reader.GetBoolean(14),
                    SubscriptionStart = reader.IsDBNull(15) ? DateTime.Now : reader.GetDateTime(15),
                    IsDeleted = !reader.IsDBNull(16) && reader.GetBoolean(16),
                };

                batch.Add((legacyId, organization));
            }
        }

        if (!context.DryRun && batch.Count > 0)
        {
            foreach (var chunk in batch.Chunk(200))
            {
                db.Set<Organization>().AddRange(chunk.Select(item => item.Entity));
                await db.SaveChangesAsync(cancellationToken);

                // Written here, with the chunk that produced it, rather than once at the end. A run
                // that dies halfway then leaves rows the next run recognises as its own; recording
                // the map late leaves orphans that collide on the next attempt.
                var chunkPairs = chunk.Select(item => (item.LegacyId, item.Entity.Id)).ToList();
                await context.IdMap.SaveAsync("Firma_T:Kurum", chunkPairs, 'I', cancellationToken);

                pairs.AddRange(chunkPairs);
            }
        }
        else if (context.DryRun)
        {
            // A rehearsal still has to hand the next stage something to hang its rows on.
            pairs.AddRange(batch.Select(item => (item.LegacyId, context.NextDryRunId())));
        }

        var note = $"organizations: {batch.Count} written, {already.Count} already there";
        if (unknownType > 0 || unknownPlan > 0)
        {
            note += $" ({unknownType} unmapped type, {unknownPlan} unmapped plan -> defaults)";
        }

        var map = already.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, batch.Count, read - batch.Count, note), map);
    }

    // ------------------------------------------------------------------ offices

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateOfficesAsync(
        MigrationContext context,
        Dictionary<int, int> organizationMap,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var cityMap = await context.IdMap.LoadAsync("Sehir_T", cancellationToken);
        var already = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);

        var read = 0;
        var orphaned = 0;
        var demoted = 0;
        var batch = new List<(int LegacyId, Office Entity)>();

        // One head office per organization is a unique index in the rebuilt schema; the legacy data
        // has two organizations flagging several. Ordering by the legacy id means the earliest keeps
        // the flag.
        var headquartersTaken = new HashSet<int>();

        await using (var seen = context.CreateDbContext())
        {
            var existing = await seen.Set<Office>()
                .Where(o => o.IsHeadquarterOffice && o.TenantId != null)
                .Select(o => o.TenantId!.Value)
                .ToListAsync(cancellationToken);

            headquartersTaken.UnionWith(existing);
        }

        const string sql = """
            SELECT OfisId, OfisAdi, Telefon, Faks, Adres, YetkiliKisi, YetkiliKisiEmail,
                   Aktif, SehirId, MerkezOfis, KurumId, EklemeTarihi, IsDeleted
            FROM Ofisler_T ORDER BY OfisId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = reader.GetInt32(0);
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                // An office whose organization did not come across has no tenant to belong to.
                // Writing it with a null TenantId would make it visible to every organization.
                if (!organizationMap.TryGetValue(reader.GetInt32(10), out var tenantId))
                {
                    orphaned++;
                    continue;
                }

                var isHeadquarters = !reader.IsDBNull(9) && reader.GetBoolean(9);
                if (isHeadquarters && !headquartersTaken.Add(tenantId))
                {
                    // The organization already has one. The office is still carried across; it just
                    // stops claiming to be the head office.
                    isHeadquarters = false;
                    demoted++;
                }

                batch.Add((legacyId, new Office
                {
                    Name = Fit(context, "Office", "Name", Text(reader, 1)) ?? $"Office {legacyId}",
                    Phone = Fit(context, "Office", "Phone", Text(reader, 2)),
                    Fax = Fit(context, "Office", "Fax", Text(reader, 3)),
                    Address = Fit(context, "Office", "Address", Text(reader, 4)),
                    AuthorizedPerson = Fit(context, "Office", "AuthorizedPerson", Text(reader, 5)),
                    AuthorizedEmail = Fit(context, "Office", "AuthorizedEmail", Text(reader, 6)),
                    IsActive = !reader.IsDBNull(7) && reader.GetBoolean(7),
                    CityId = MapId(cityMap, Int(reader, 8)),
                    IsHeadquarterOffice = isHeadquarters,
                    TenantId = tenantId,
                    IsDeleted = !reader.IsDBNull(12) && reader.GetBoolean(12),
                }));
            }
        }

        var pairs = new List<(int, int)>();

        if (!context.DryRun && batch.Count > 0)
        {
            foreach (var chunk in batch.Chunk(200))
            {
                db.Set<Office>().AddRange(chunk.Select(item => item.Entity));
                await db.SaveChangesAsync(cancellationToken);

                // Written here, with the chunk that produced it, rather than once at the end. A run
                // that dies halfway then leaves rows the next run recognises as its own; recording
                // the map late leaves orphans that collide on the next attempt.
                var chunkPairs = chunk.Select(item => (item.LegacyId, item.Entity.Id)).ToList();
                await context.IdMap.SaveAsync("Ofisler_T", chunkPairs, 'I', cancellationToken);

                pairs.AddRange(chunkPairs);
            }
        }
        else if (context.DryRun)
        {
            // A rehearsal still has to hand the next stage something to hang its rows on.
            pairs.AddRange(batch.Select(item => (item.LegacyId, context.NextDryRunId())));
        }

        var note = $"offices: {batch.Count} written";
        if (demoted > 0)
        {
            note += $", {demoted} demoted from head office (one per organization)";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (organization missing)";
        }

        var map = already.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, batch.Count, read - batch.Count, note), map);
    }

    // ------------------------------------------------------------------ users

    private static async Task<StepResult> MigrateUsersAsync(
        MigrationContext context,
        Dictionary<int, int> organizationMap,
        Dictionary<int, int> officeMap,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var cityMap = await context.IdMap.LoadAsync("Sehir_T", cancellationToken);
        var districtMap = await context.IdMap.LoadAsync("Ilce_T", cancellationToken);
        var already = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);

        var takenUserNames = await db.Set<User>()
            .Select(u => u.NormalizedUserName!)
            .ToListAsync(cancellationToken);

        var taken = new HashSet<string>(takenUserNames.Select(CollationFold), StringComparer.Ordinal);

        var read = 0;
        var orphaned = 0;
        var invented = 0;
        var duplicateNationalIds = 0;
        var unreadable = 0;
        // The account, and the three rows that describe the person it belongs to. They are
        // built together and written together, because a user without a profile cannot act:
        // authorization reads whether an account is usable from there.
        var batch = new List<(int LegacyId, User Entity, UserProfile Profile,
            UserEmployment Employment, UserMedulaCredential? Medula, UserOffice? Office)>();

        // (organization, national id) is unique in the rebuilt schema. Whoever comes first keeps
        // the number; a later account with the same one is written without it.
        var nationalIdsTaken = new HashSet<(int TenantId, string NationalId)>();

        foreach (var existing in await db.Set<UserProfile>()
                     .Where(p => p.NationalId != null && p.TenantId != null)
                     .Select(p => new { p.TenantId, p.NationalId })
                     .ToListAsync(cancellationToken))
        {
            nationalIdsTaken.Add((existing.TenantId!.Value, existing.NationalId!));
        }

        const string sql = """
            SELECT KullaniciId, Adi, Soyadi, Email, TCKimlikNo, Telefon, GSM, Adres,
                   SehirId, IlceId, Aktif, Admin, SerAdmin, OfisAdmin, PersonelTuru,
                   IseGirisTarihi, IstenCikisTarihi, BrutMaas, PartTime, CalismaSuresi,
                   OfisId, FirmaId, KurumId, Renk, BransKodu, MedulaKullanici, MedulaSifre,
                   SozlesmeOnaylandi, EklemeTarihi, IsDeleted
            FROM Kullanici_T ORDER BY KullaniciId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = reader.GetInt32(0);
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!organizationMap.TryGetValue(reader.GetInt32(22), out var tenantId))
                {
                    orphaned++;
                    continue;
                }

                var email = Text(reader, 3);
                var userName = ResolveUserName(email, legacyId, taken, ref invented);

                var staffRoleText = Text(reader, 14);
                var staffRole = staffRoleText is not null && StaffRoles.TryGetValue(staffRoleText, out var mapped)
                    ? mapped
                    : StaffRole.Unspecified;

                // Already ciphertext in the legacy database, under the legacy key. Read it as
                // itself first: writing it through unchanged would encrypt it a second time, and
                // the result is unreadable by anything while looking perfectly migrated.
                var legacyNationalId = Text(reader, 4);
                var nationalId = Fit(context, "User", "NationalId", LegacyCrypt.TryDecrypt(legacyNationalId));

                if (legacyNationalId is not null && nationalId is null && LegacyCrypt.LooksEncrypted(legacyNationalId))
                {
                    unreadable++;
                }
                if (nationalId is not null && !nationalIdsTaken.Add((tenantId, nationalId)))
                {
                    nationalId = null;
                    duplicateNationalIds++;
                }

                var user = new User
                {
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email?.ToUpperInvariant(),
                    PhoneNumber = Fit(context, "User", "PhoneNumber", Text(reader, 5))
                                  ?? Fit(context, "User", "PhoneNumber", Text(reader, 6)),

                    TenantId = tenantId,

                    // No password is carried over here. The legacy column holds reversibly
                    // encrypted text rather than a hash, so PasswordStep decrypts and re-protects
                    // it in its own pass; until then the account is reachable only through a reset.
                    PasswordHash = null,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    LockoutEnabled = true,
                    EmailConfirmed = false,
                };

                var profile = new UserProfile
                {
                    TenantId = tenantId,
                    Name = Fit(context, "UserProfile", "Name", Text(reader, 1)) ?? string.Empty,
                    LastName = Fit(context, "UserProfile", "LastName", Text(reader, 2)) ?? string.Empty,
                    NationalId = nationalId,
                    Address = Fit(context, "UserProfile", "Address", Text(reader, 7)),
                    CityId = MapId(cityMap, Int(reader, 8)),
                    DistrictId = MapId(districtMap, Int(reader, 9)),
                    Color = Fit(context, "UserProfile", "Color", Text(reader, 23)),
                    IsActive = !reader.IsDBNull(10) && reader.GetBoolean(10),
                    IsContractApproved = Int(reader, 27) is > 0,

                    // FirmaId points at a client company, which the companies step has not created
                    // yet. It is resolved there rather than left pointing at nothing.
                    CompanyId = null,
                    MustChangePassword = true,
                    IsDeleted = !reader.IsDBNull(29) && reader.GetBoolean(29),
                };

                var employment = new UserEmployment
                {
                    TenantId = tenantId,
                    HireDate = Date(reader, 15),
                    TerminationDate = Date(reader, 16),
                    GrossSalary = reader.IsDBNull(17) ? null : (decimal)reader.GetDouble(17),
                    PartTime = Int(reader, 18) is > 0,
                    IsDeleted = !reader.IsDBNull(29) && reader.GetBoolean(29),
                };

                // Only for the users that have one. An empty row per user would be storing
                // nothing, several thousand times.
                var medulaUserName = Fit(context, "UserMedulaCredential", "MedulaUserName",
                    LegacyCrypt.TryDecrypt(Text(reader, 25)));
                var medulaPassword = Fit(context, "UserMedulaCredential", "MedulaPassword",
                    LegacyCrypt.TryDecrypt(Text(reader, 26)));
                var medicalSpecialtyCode = Fit(context, "UserMedulaCredential", "MedicalSpecialtyCode", Text(reader, 24));

                var medula = medulaUserName is null && medulaPassword is null && medicalSpecialtyCode is null
                    ? null
                    : new UserMedulaCredential
                    {
                        TenantId = tenantId,
                        MedulaUserName = medulaUserName,
                        MedulaPassword = medulaPassword,
                        MedicalSpecialtyCode = medicalSpecialtyCode,
                    };

                // The office the legacy row names, written as an assignment.
                // Kullanici_T.OfisId and KullaniciOfis_T are two different facts in the legacy
                // schema -- a per-user default beside a many-to-many table -- and carrying only
                // the second one is how 1,907 people lost their office the first time round.
                var office = MapId(officeMap, Int(reader, 20)) is { } assignedOfficeId
                    ? new UserOffice
                    {
                        TenantId = tenantId,
                        OfficeId = assignedOfficeId,
                        MonthlyWorkDurationMinutes = Int(reader, 19) ?? 0,
                    }
                    : null;

                taken.Add(CollationFold(user.NormalizedUserName));
                batch.Add((legacyId, user, profile, employment, medula, office));
            }
        }

        var pairs = new List<(int, int)>();

        if (!context.DryRun && batch.Count > 0)
        {
            foreach (var chunk in batch.Chunk(200))
            {
                db.Set<User>().AddRange(chunk.Select(item => item.Entity));
                await db.SaveChangesAsync(cancellationToken);

                // Now the accounts have ids, so the rows that point at them can be written.
                foreach (var item in chunk)
                {
                    item.Profile.UserId = item.Entity.Id;
                    item.Employment.UserId = item.Entity.Id;

                    db.Set<UserProfile>().Add(item.Profile);
                    db.Set<UserEmployment>().Add(item.Employment);

                    if (item.Medula is { } credential)
                    {
                        credential.UserId = item.Entity.Id;
                        db.Set<UserMedulaCredential>().Add(credential);
                    }

                    if (item.Office is { } assignment)
                    {
                        assignment.UserId = item.Entity.Id;
                        db.Set<UserOffice>().Add(assignment);
                    }
                }

                await db.SaveChangesAsync(cancellationToken);

                // Written here, with the chunk that produced it, rather than once at the end. A run
                // that dies halfway then leaves rows the next run recognises as its own; recording
                // the map late leaves orphans that collide on the next attempt.
                var chunkPairs = chunk.Select(item => (item.LegacyId, item.Entity.Id)).ToList();
                await context.IdMap.SaveAsync("Kullanici_T", chunkPairs, 'I', cancellationToken);

                pairs.AddRange(chunkPairs);
            }
        }

        var note = $"users: {batch.Count} written, all with MustChangePassword (no password is migrated)";
        if (invented > 0)
        {
            note += $", {invented} user name(s) derived (missing or duplicate e-mail)";
        }

        if (unreadable > 0)
        {
            note += $", {unreadable} national id(s) UNREADABLE (legacy ciphertext this key cannot open)";
        }

        if (duplicateNationalIds > 0)
        {
            note += $", {duplicateNationalIds} national id(s) DROPPED as duplicates within their organization";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (organization missing)";
        }

        return new StepResult(read, batch.Count, read - batch.Count, note);
    }

    /// <summary>
    /// Produces a unique user name.
    /// <para>
    /// The legacy application signed people in by e-mail and stored no user name. That does not
    /// survive contact with the data: 600 accounts have no e-mail and 208 addresses belong to more
    /// than one account, while Identity needs one unique name each.
    /// </para>
    /// <para>
    /// So: the e-mail when it is free, otherwise the e-mail with the legacy id appended, otherwise
    /// the legacy id alone. Every fallback keeps the legacy id visible, so an account whose name
    /// was invented can be traced back to the row it came from.
    /// </para>
    /// </summary>
    private static string ResolveUserName(
        string? email,
        int legacyId,
        HashSet<string> taken,
        ref int invented)
    {
        if (!string.IsNullOrWhiteSpace(email) && !taken.Contains(CollationFold(email.Trim())))
        {
            return email.Trim();
        }

        invented++;

        if (!string.IsNullOrWhiteSpace(email))
        {
            var candidate = $"{email.Trim()}.{legacyId}";
            if (!taken.Contains(CollationFold(candidate)))
            {
                return candidate;
            }
        }

        return $"legacy.{legacyId}";
    }

    /// <summary>
    /// Folds a name the way the database's <c>Turkish_CI_AS</c> collation compares it.
    /// <para>
    /// <c>ToUpperInvariant</c> is not enough. It turns <c>i</c> into <c>I</c>, which in a Turkish
    /// collation is the same letter as the dotless <c>ı</c> - so two user names this step thought
    /// were distinct were duplicates as far as the unique index was concerned, and the run failed
    /// part-way through 3,878 accounts.
    /// </para>
    /// <para>
    /// Slightly stricter than the index, on purpose: folding too much costs an unnecessary suffix
    /// on somebody's user name, folding too little costs a failed migration.
    /// </para>
    /// </summary>
    private static string CollationFold(string value)
        => value.Trim()
            .Replace('İ', 'i').Replace('I', 'ı')
            .ToLowerInvariant()
            .Replace('ı', 'i');

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Shortens a value to what the destination column holds, counting it when it did not fit.
    /// <para>
    /// Legacy free text does not respect the field it was typed into - one organization's
    /// "authorised person's telephone" holds an e-mail address. Truncating and reporting beats both
    /// failing the whole run and losing the characters quietly.
    /// </para>
    /// </summary>
    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

    private static string? Text(SqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index)?.ToString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int? Int(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : Convert.ToInt32(reader.GetValue(index));

    private static DateTime? Date(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetDateTime(index);

    /// <summary>Translates a legacy foreign key, or null when it did not come across.</summary>
    private static int? MapId(Dictionary<int, int> map, int? legacyId)
        => legacyId is { } id && map.TryGetValue(id, out var modernId) ? modernId : null;
}
