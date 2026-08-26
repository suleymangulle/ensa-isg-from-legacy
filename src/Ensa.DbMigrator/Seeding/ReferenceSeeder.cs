using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Ensa.Domain.Lookups;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// Loads the tenant-independent (host) reference data: cities, organization, subscription plan
/// and user types, and the permission catalogue.
/// </summary>
public sealed class ReferenceSeeder(EnsaDbContext context, ILogger<ReferenceSeeder> logger) : IDataSeeder
{
    public int Order => 100;

    public string Name => "Reference data (cities, types, permissions)";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCitiesAsync(cancellationToken);
        await SeedOrganizationTypesAsync(cancellationToken);
        await SeedPackageTypesAsync(cancellationToken);
        await SeedUserTypesAsync(cancellationToken);
        await SeedPermissionsAsync(cancellationToken);
    }

    // --------------------------------------------------------------
    // Cities — 81 provinces, index order = plate code
    // --------------------------------------------------------------

    private static readonly string[] CityNames =
    [
        "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Amasya", "Ankara", "Antalya",
        "Artvin", "Aydın", "Balıkesir", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur",
        "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Edirne",
        "Elazığ", "Erzincan", "Erzurum", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane",
        "Hakkâri", "Hatay", "Isparta", "Mersin", "İstanbul", "İzmir", "Kars", "Kastamonu",
        "Kayseri", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya",
        "Manisa", "Kahramanmaraş", "Mardin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu",
        "Rize", "Sakarya", "Samsun", "Siirt", "Sinop", "Sivas", "Tekirdağ", "Tokat",
        "Trabzon", "Tunceli", "Şanlıurfa", "Uşak", "Van", "Yozgat", "Zonguldak", "Aksaray",
        "Bayburt", "Karaman", "Kırıkkale", "Batman", "Şırnak", "Bartın", "Ardahan", "Iğdır",
        "Yalova", "Karabük", "Kilis", "Osmaniye", "Düzce"
    ];

    private async Task SeedCitiesAsync(CancellationToken cancellationToken)
    {
        var existingPlateCodes = await context.Set<City>()
            .Select(s => s.PlateCodeCode)
            .ToListAsync(cancellationToken);

        var toInsert = new List<City>();

        for (var i = 0; i < CityNames.Length; i++)
        {
            var plateCode = i + 1;
            if (existingPlateCodes.Contains(plateCode))
            {
                continue;
            }

            toInsert.Add(new City { CityName = CityNames[i], PlateCodeCode = plateCode });
        }

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<City>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} cities inserted.", toInsert.Count);
    }

    // --------------------------------------------------------------
    // Organization and subscription plan types
    // --------------------------------------------------------------

    private async Task SeedOrganizationTypesAsync(CancellationToken cancellationToken)
    {
        (string Code, string Name)[] types =
        [
            ("OSGB", "Joint Health and Safety Unit"),
            ("ISGB", "Workplace Health and Safety Unit"),
            ("BIREYSEL", "Individual"),
            ("KAMU", "Public Institution")
        ];

        var current = await context.Set<OrganizationType>()
            .Select(k => k.Code)
            .ToListAsync(cancellationToken);

        var toInsert = types
            .Where(t => !current.Contains(t.Code))
            .Select((t, i) => new OrganizationType
            {
                Code = t.Code,
                Name = t.Name,
                SortOrder = (i + 1) * 10,
                IsActive = true
            })
            .ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<OrganizationType>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} organization types inserted.", toInsert.Count);
    }

    private async Task SeedPackageTypesAsync(CancellationToken cancellationToken)
    {
        (string Code, string Name)[] types =
        [
            ("DEMO", "Demo"),
            ("BASLANGIC", "Starter"),
            ("STANDART", "Standard"),
            ("PROFESYONEL", "Professional"),
            ("KURUMSAL", "Enterprise")
        ];

        var current = await context.Set<SubscriptionPlan>()
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        var toInsert = types
            .Where(t => !current.Contains(t.Code))
            .Select((t, i) => new SubscriptionPlan
            {
                Code = t.Code,
                Name = t.Name,
                SortOrder = (i + 1) * 10,
                IsActive = true
            })
            .ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<SubscriptionPlan>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} subscription plans inserted.", toInsert.Count);
    }

    private async Task SeedUserTypesAsync(CancellationToken cancellationToken)
    {
        (string Code, string Name, StaffRole Type)[] types =
        [
            ("SISTEM-YONETICISI", "System Administrator", StaffRole.SystemAdministrator),
            ("KURUM-YONETICISI", "Organization Administrator", StaffRole.OrganizationAdministrator),
            ("OFIS-YONETICISI", "Office Administrator", StaffRole.OfficeAdministrator),
            ("UZMAN", "Occupational Safety Specialist", StaffRole.OccupationalSafetySpecialist),
            ("HEKIM", "Workplace Physician", StaffRole.WorkplacePhysician),
            ("DSP", "Other Health Personnel", StaffRole.OtherHealthPersonnel),
            ("BURO", "Office Staff", StaffRole.OfficeStaff),
            ("MUSTERI", "Customer", StaffRole.Customer)
        ];

        var current = await context.Set<UserType>()
            .Select(k => k.Code)
            .ToListAsync(cancellationToken);

        var toInsert = types
            .Where(t => !current.Contains(t.Code))
            .Select((t, i) => new UserType
            {
                Code = t.Code,
                Name = t.Name,
                StaffRole = t.Type,
                SortOrder = (i + 1) * 10,
                IsActive = true
            })
            .ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<UserType>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} user types inserted.", toInsert.Count);
    }

    // --------------------------------------------------------------
    // Permission catalogue — derived from the EnsaPermissions constants
    // --------------------------------------------------------------

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingTargets = await context.Set<Permission>()
            .Select(y => y.PermissionTarget)
            .ToListAsync(cancellationToken);

        var order = 0;
        var toInsert = new List<Permission>();

        foreach (var permission in EnsaPermissions.GetAll())
        {
            order += 10;

            if (existingTargets.Contains(permission))
            {
                continue;
            }

            toInsert.Add(new Permission
            {
                PermissionTarget = permission,
                PermissionName = PermissionName(permission),
                PermissionType = PermissionType.MethodPermission,
                PermissionRestrictionMode = PermissionRestrictionMode.Everyone,
                SortOrder = order
            });
        }

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<Permission>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} permissions inserted.", toInsert.Count);
    }

    /// <summary>
    /// Turns a permission code into a readable catalogue name:
    /// <c>"Ensa.Company.Create"</c> becomes <c>"Company - Create"</c>.
    /// <para>
    /// This is only the seeded display label. What the user actually sees in the UI comes from
    /// the localization resources, keyed by the permission code itself.
    /// </para>
    /// </summary>
    private static string PermissionName(string permission)
    {
        var segments = permission.Split('.');
        if (segments.Length < 3)
        {
            return permission;
        }

        var module = segments[1];
        var action = segments[^1];

        return $"{module} - {action}";
    }
}
