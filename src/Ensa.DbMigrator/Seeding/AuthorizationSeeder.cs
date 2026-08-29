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

    /// <summary>
    /// Screens a customer contact must be able to see, named by the legacy page permission that
    /// now governs the menu entry (ADR-040).
    /// <para>
    /// <b>This is configuration, not migration, and it is written down rather than inferred.</b>
    /// The legacy <c>KullaniciTypeYetki_T</c> holds grants for six user types and none for
    /// <c>Musteri</c> - 286 users with no permission row anywhere, because legacy decided customer
    /// access with a hand-written <c>PersonelTuru == "Musteri"</c> branch inside each controller
    /// rather than with the permission tables (ADR-042). Migrating faithfully therefore carries
    /// over nothing at all for them, and a customer would sign in to a navigation bar holding the
    /// dashboard and four screens they have no business seeing.
    /// </para>
    /// <para>
    /// The list is exactly the customer portal of ADR-037 and nothing else. It grants visibility;
    /// what a customer may actually do is decided by the endpoint gate and narrowed to their own
    /// workplace by the company scope filter (ADR-034), neither of which reads this table.
    /// </para>
    /// </summary>
    private static readonly (string UserTypeCode, string[] PermissionTargets)[] CustomerPortalGrants =
    [
        ("MUSTERI",
        [
            "ENSA_ISG.FirmaListController",                              // their own workplace
            "ENSA_ISG.FirmaPersonelListController",                      // its employees
            "ENSA_ISG.FirmaBolumListController",                         // its departments
            "ENSA_ISG.FirmaCihazListController",                         // its equipment
            "ENSA_ISG.Controllers.EgitimKatilimSertifikasiController",   // missing trainings
            "ENSA_ISG.DosyaController",                                  // inspection documents
        ]),
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
        var byTarget = permissions.ToDictionary(p => p.PermissionTarget, p => p.Id);

        await SeedStaffTypeDefaultsAsync(byTarget, cancellationToken);
        await SeedCustomerPortalGrantsAsync(byTarget, cancellationToken);
    }

    /// <summary>
    /// Gives the customer user type the visibility legacy never recorded. Additive and idempotent:
    /// it inserts the rows that are missing and touches nothing else, so an administrator who has
    /// since removed one does not get it back on the next run.
    /// </summary>
    private async Task SeedCustomerPortalGrantsAsync(
        Dictionary<string, int> permissionsByTarget,
        CancellationToken cancellationToken)
    {
        var toInsert = new List<UserTypePermission>();
        var unknown = new List<string>();

        foreach (var (userTypeCode, targets) in CustomerPortalGrants)
        {
            var userType = await context.Set<UserType>()
                .Where(type => type.Code == userTypeCode)
                .Select(type => new { type.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (userType is null)
            {
                continue;
            }

            var held = await context.Set<UserTypePermission>()
                .Where(row => row.UserTypeId == userType.Id)
                .Select(row => row.PermissionId)
                .ToListAsync(cancellationToken);

            var heldIds = held.ToHashSet();

            foreach (var target in targets)
            {
                if (!permissionsByTarget.TryGetValue(target, out var permissionId))
                {
                    // The legacy data has not been migrated into this database; nothing to grant.
                    unknown.Add(target);
                    continue;
                }

                await PermitRestrictionAsync(permissionId, userType.Id, target, cancellationToken);

                if (heldIds.Add(permissionId))
                {
                    toInsert.Add(new UserTypePermission
                    {
                        UserTypeId = userType.Id,
                        PermissionId = permissionId,
                        IsActive = true,
                    });
                }
            }
        }

        if (unknown.Count > 0)
        {
            logger.LogInformation(
                "{Count} customer-portal permission(s) are not present in this database and were "
                + "skipped; they arrive with the legacy migration.", unknown.Count);
        }

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<UserTypePermission>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} customer-portal permission row(s) inserted so a customer's navigation is not "
            + "empty; legacy recorded none for that user type.", toInsert.Count);
    }

    /// <summary>
    /// Makes sure a <see cref="PermissionRestriction"/> does not veto the grant just made.
    /// <para>
    /// The sixth gate of the legacy algorithm drops a permission whose
    /// <see cref="PermissionRestrictionMode"/> is <c>OnlySelected</c> when the user type is absent
    /// from its list. Two of the customer-portal screens carry exactly that:
    /// <c>FirmaListController</c> and <c>EgitimKatilimSertifikasiController</c> are marked
    /// "only these user types", and <c>Musteri</c> is not among them.
    /// </para>
    /// <para>
    /// <b>Why overriding it is the faithful reading.</b> That restriction was authored in the same
    /// administration screen as the rest, and it never ran either (ADR-042) - legacy showed a
    /// customer their own workplace and their missing trainings through a hand-written branch, so
    /// the two screens the rule forbids are two the shipped product served. Taking the rule
    /// literally would remove a working feature on the strength of configuration nothing enforced.
    /// The rule is left in place for every other user type; only the row that unblocks the portal
    /// is added.
    /// </para>
    /// </summary>
    private async Task PermitRestrictionAsync(
        int permissionId,
        int userTypeId,
        string target,
        CancellationToken cancellationToken)
    {
        var mode = await context.Set<Permission>()
            .Where(permission => permission.Id == permissionId)
            .Select(permission => permission.PermissionRestrictionMode)
            .FirstOrDefaultAsync(cancellationToken);

        if (mode != PermissionRestrictionMode.OnlySelected)
        {
            // Everyone: nothing vetoes it. SelectedExcept: a veto would mean the customer is named
            // in a deny list, which is a deliberate exclusion rather than an unfinished allow list;
            // it is reported instead of edited.
            if (mode == PermissionRestrictionMode.SelectedExcept
                && await context.Set<PermissionRestriction>().AnyAsync(
                       row => row.PermissionId == permissionId && row.UserTypeId == userTypeId,
                       cancellationToken))
            {
                logger.LogWarning(
                    "{Target} excludes this user type explicitly; the customer portal entry stays "
                    + "hidden. Remove the restriction row if that is not intended.", target);
            }

            return;
        }

        var alreadyPermitted = await context.Set<PermissionRestriction>()
            .AnyAsync(row => row.PermissionId == permissionId && row.UserTypeId == userTypeId,
                      cancellationToken);

        if (alreadyPermitted)
        {
            return;
        }

        context.Set<PermissionRestriction>().Add(new PermissionRestriction
        {
            PermissionId = permissionId,
            UserTypeId = userTypeId,
        });

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Target} is restricted to selected user types and did not list the customer; the "
            + "customer portal needs it, so it was added to that list.", target);
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
