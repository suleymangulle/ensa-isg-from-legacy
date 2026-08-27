using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ensa.Domain.Shared;


namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// Creates the system roles, the host administrator and the demo organization.
/// <para>
/// The administrator password is read from the <c>Seed:AdminPassword</c> setting. When the
/// setting is absent the development default is used and <c>MustChangePassword</c> is set.
/// </para>
/// </summary>
public sealed class MembershipSeeder(
    EnsaDbContext context,
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IConfiguration configuration,
    ILogger<MembershipSeeder> logger) : IDataSeeder
{
    private const string DefaultAdminPassword = "Ensa!2026";

    public int Order => 200;

    public string Name => "Membership (roles, administrator, demo organization)";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        var organizationId = await SeedDemoOrganizationAsync(cancellationToken);
        await SeedAdminAsync(organizationId, cancellationToken);
    }

    // --------------------------------------------------------------
    // System roles
    // --------------------------------------------------------------

    private static readonly (string Name, string Description)[] Roles =
    [
        ("SystemAdministrator", "Host administrator with access to every organization and every permission"),
        ("OrganizationAdministrator", "Manages all data belonging to their own organization"),
        ("OfficeAdministrator", "Manages the data of the office they belong to"),
        ("Specialist", "Occupational safety specialist"),
        ("Physician", "Workplace physician"),
        ("Office", "Office staff"),
        ("Customer", "Client company user")
    ];

    private async Task SeedRolesAsync()
    {
        foreach (var (name, description) in Roles)
        {
            if (await roleManager.RoleExistsAsync(name))
            {
                continue;
            }

            var role = new Role
            {
                Name = name,
                Description = description,
                IsStatic = true,
                TenantId = null
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role '{name}': {ErrorsCombine(result)}");
            }

            logger.LogInformation("Role created: {Role}", name);
        }
    }

    // --------------------------------------------------------------
    // Demo organization (tenant)
    // --------------------------------------------------------------

    private async Task<int> SeedDemoOrganizationAsync(CancellationToken cancellationToken)
    {
        const string code = "DEMO";

        var current = await context.Set<Organization>()
            .FirstOrDefaultAsync(k => k.Code == code, cancellationToken);

        if (current is not null)
        {
            return current.Id;
        }

        var organizationTypeId = await context.Set<OrganizationType>()
            .Where(k => k.Code == "OSGB")
            .Select(k => k.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var subscriptionPlanId = await context.Set<SubscriptionPlan>()
            .Where(p => p.Code == "KURUMSAL")
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizationTypeId == 0 || subscriptionPlanId == 0)
        {
            throw new InvalidOperationException(
                "Organization and subscription plan types are missing — ReferenceSeeder must run first.");
        }

        var organization = new Organization
        {
            Code = code,
            Name = "Demo OHS Provider",
            OrganizationTypeId = organizationTypeId,
            SubscriptionPlanId = subscriptionPlanId,
            SubscriptionStart = DateTime.Now.Date,
            IsActive = true
        };

        context.Set<Organization>().Add(organization);
        await context.SaveChangesAsync(cancellationToken);

        var office = new Office
        {
            Name = "Head Office",
            HeadquarterOffice = true,
            IsActive = true,
            TenantId = organization.Id
        };

        context.Set<Office>().Add(office);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo organization created (Id = {OrganizationId}).", organization.Id);
        return organization.Id;
    }

    // --------------------------------------------------------------
    // Host administrator
    // --------------------------------------------------------------

    private async Task SeedAdminAsync(int demoOrganizationId, CancellationToken cancellationToken)
    {
        const string userName = "admin";

        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var configuredPassword = configuration["Seed:AdminPassword"];
        var password = string.IsNullOrWhiteSpace(configuredPassword) ? DefaultAdminPassword : configuredPassword;

        var admin = new User
        {
            UserName = userName,
            Email = "admin@ensa.local",
            EmailConfirmed = true,
            // The host administrator belongs to no tenant; it manages every organization.
            TenantId = null
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the administrator user: {ErrorsCombine(result)}");
        }

        // The person and the contract. Authorization reads whether an account is usable from the
        // profile, so an administrator created without one could sign in and then do nothing --
        // which, for the account that has to fix everything else, would be a bad first impression.
        context.Add(new UserProfile
        {
            UserId = admin.Id,
            Name = "System",
            LastName = "Administrator",
            IsActive = true,
            MustChangePassword = string.IsNullOrWhiteSpace(configuredPassword)
        });

        var administratorType = await context.Set<UserType>()
            .FirstOrDefaultAsync(t => t.StaffRole == StaffRole.SystemAdministrator, cancellationToken);

        context.Add(new UserEmployment
        {
            UserId = admin.Id,
            UserTypeId = administratorType?.Id
        });

        await context.SaveChangesAsync(cancellationToken);

        // System administrator and organization administrator are role assignments now, not
        // booleans on the account.
        foreach (var roleName in new[]
                 {
                     EnsaRoleNames.SystemAdministrator,
                     EnsaRoleNames.OrganizationAdministrator,
                 })
        {
            if (!await userManager.IsInRoleAsync(admin, roleName))
            {
                await userManager.AddToRoleAsync(admin, roleName);
            }
        }

        var roleResult = await userManager.AddToRoleAsync(admin, "SystemAdministrator");
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign the role to the administrator: {ErrorsCombine(roleResult)}");
        }

        logger.LogInformation(
            "Administrator created. User name: {UserName}. Demo organization Id: {OrganizationId}. " +
            "Permission count: {PermissionCount} (SystemAdministrator holds every permission).",
            userName,
            demoOrganizationId,
            EnsaPermissions.GetAll().Count());

        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            logger.LogWarning(
                "The administrator password is the built-in default and must be changed on first sign-in. " +
                "Define 'Seed:AdminPassword' (or the Seed__AdminPassword environment variable) outside development.");
        }
    }

    private static string ErrorsCombine(IdentityResult result)
        => string.Join(" · ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
