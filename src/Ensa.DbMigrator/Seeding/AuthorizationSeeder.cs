using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// Opens the authorization gates and gives each staff type a usable baseline.
/// <para>
/// <b>Why this exists.</b> <c>IPermissionManager</c> applies two gates before it looks at anything
/// granted to a user: a permission that is not opened to the organization's <c>SubscriptionPlan</c>
/// is dropped, and so is one not opened to its <c>OrganizationType</c>. Both gates fail closed —
/// an empty table means "nothing passes". With neither table seeded, every non-administrator user
/// ended up with an empty permission set no matter what was granted to them, and the only account
/// that worked was the seeded administrator, which bypasses the whole calculation through the
/// system-administrator shortcut. A fresh installation could not produce a working specialist,
/// physician or customer.
/// </para>
/// <para>
/// <b>What it seeds, and why that shape.</b> Both gates are opened fully: they exist to <i>narrow</i>
/// a plan or an organization type, so their honest default is transparent — they restrict nothing
/// until somebody configures them. Narrowing a subscription plan is then a deliberate commercial
/// act rather than an accident of an unseeded table.
/// </para>
/// <para>
/// Staff-type defaults are deliberately conservative: the two administrator types get everything,
/// and every other type gets each module's <c>Default</c> — permission to view and list — and
/// nothing that writes. Guessing which role may delete an invoice is a decision for the customer,
/// not for a seeder, and it is now editable through <c>PUT api/permission/user-type/{id}</c>.
/// </para>
/// </summary>
public class AuthorizationSeeder(EnsaDbContext context, ILogger<AuthorizationSeeder> logger)
    : IDataSeeder
{
    /// <summary>Runs after the reference data, which creates the permissions and the types.</summary>
    public int Order => 150;

    public string Name => "Authorization gates and staff-type defaults";

    /// <summary>Staff types that administer the product and therefore receive everything.</summary>
    private static readonly StaffRole[] AdministratorRoles =
    [
        StaffRole.SystemAdministrator,
        StaffRole.OrganizationAdministrator,
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await context.Set<Permission>()
            .Select(permission => new { permission.Id, permission.PermissionTarget })
            .ToListAsync(cancellationToken);

        if (permissions.Count == 0)
        {
            logger.LogWarning("No permissions found; the authorization seeder has nothing to open.");
            return;
        }

        var permissionIds = permissions.ConvertAll(permission => permission.Id);

        await OpenSubscriptionPlansAsync(permissionIds, cancellationToken);
        await OpenOrganizationTypesAsync(permissionIds, cancellationToken);
        await SeedStaffTypeDefaultsAsync(permissions.ToDictionary(p => p.PermissionTarget, p => p.Id), cancellationToken);
    }

    /// <summary>Every plan opens every permission until somebody narrows it.</summary>
    private async Task OpenSubscriptionPlansAsync(List<int> permissionIds, CancellationToken cancellationToken)
    {
        var planIds = await context.Set<SubscriptionPlan>()
            .Select(plan => plan.Id)
            .ToListAsync(cancellationToken);

        var existing = await context.Set<SubscriptionPlanPermission>()
            .Select(row => new { row.SubscriptionPlanId, row.PermissionId })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(row => (row.SubscriptionPlanId, row.PermissionId))
            .ToHashSet();

        var toInsert = (from planId in planIds
                        from permissionId in permissionIds
                        where !known.Contains((planId, permissionId))
                        select new SubscriptionPlanPermission
                        {
                            SubscriptionPlanId = planId,
                            PermissionId = permissionId,
                        }).ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<SubscriptionPlanPermission>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} subscription-plan permission rows inserted for {PlanCount} plan(s).",
            toInsert.Count, planIds.Count);
    }

    /// <summary>Every organization type opens every permission until somebody narrows it.</summary>
    private async Task OpenOrganizationTypesAsync(List<int> permissionIds, CancellationToken cancellationToken)
    {
        var typeIds = await context.Set<OrganizationType>()
            .Select(type => type.Id)
            .ToListAsync(cancellationToken);

        var existing = await context.Set<OrganizationTypePermission>()
            .Select(row => new { row.OrganizationTypeId, row.PermissionId })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(row => (row.OrganizationTypeId, row.PermissionId))
            .ToHashSet();

        var toInsert = (from typeId in typeIds
                        from permissionId in permissionIds
                        where !known.Contains((typeId, permissionId))
                        select new OrganizationTypePermission
                        {
                            OrganizationTypeId = typeId,
                            PermissionId = permissionId,
                        }).ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<OrganizationTypePermission>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} organization-type permission rows inserted for {TypeCount} type(s).",
            toInsert.Count, typeIds.Count);
    }

    /// <summary>
    /// Administrator types get everything; every other type gets each module's view permission.
    /// </summary>
    private async Task SeedStaffTypeDefaultsAsync(
        Dictionary<string, int> permissionsByTarget,
        CancellationToken cancellationToken)
    {
        var userTypes = await context.Set<UserType>()
            .Select(type => new { type.Id, type.StaffRole })
            .ToListAsync(cancellationToken);

        if (userTypes.Count == 0)
        {
            return;
        }

        var alreadyConfigured = await context.Set<UserTypePermission>()
            .Select(row => row.UserTypeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allIds = permissionsByTarget.Values.ToList();

        // A module's Default is the permission with no operation suffix: "Ensa.Company", not
        // "Ensa.Company.Create". Counting the dots is enough to tell them apart.
        var viewOnlyIds = permissionsByTarget
            .Where(entry => entry.Key.Count(character => character == '.') == 1)
            .Select(entry => entry.Value)
            .ToList();

        var toInsert = new List<UserTypePermission>();

        foreach (var userType in userTypes)
        {
            // Never overwrite a configured type: the seeder is idempotent and must not undo
            // decisions an administrator has already made.
            if (alreadyConfigured.Contains(userType.Id))
            {
                continue;
            }

            var ids = AdministratorRoles.Contains(userType.StaffRole) ? allIds : viewOnlyIds;

            toInsert.AddRange(ids.Select(permissionId => new UserTypePermission
            {
                UserTypeId = userType.Id,
                PermissionId = permissionId,
                IsActive = true,
            }));
        }

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<UserTypePermission>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} staff-type permission rows inserted. Non-administrator types receive view "
            + "permissions only; refine them with PUT api/permission/user-type/{{id}}.",
            toInsert.Count);
    }
}
