using Ensa.Domain.Repositories;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;

namespace Ensa.Domain.Membership;

/// <summary>
/// Contract of the domain service that computes a user's EFFECTIVE permissions.
/// </summary>
public interface IPermissionManager : IDomainService
{
    /// <summary>Computes the complete set of the user's effective permissions.</summary>
    Task<List<Permission>> GetEffectivePermissionsAsync(int userId, CancellationToken ct = default);

    /// <summary>Ids of the user's effective permissions (fast path when the permission objects are not needed).</summary>
    Task<HashSet<int>> GetEffectivePermissionIdsAsync(int userId, CancellationToken ct = default);

    /// <summary>Determines whether the user holds the permission targeting <paramref name="permissionTarget"/>.</summary>
    Task<bool> IsAuthorizedAsync(int userId, string permissionTarget, CancellationToken ct = default);

    /// <summary>Target names of the user's effective permissions — for token/claim generation.</summary>
    Task<List<string>> GetPermissionTargetsAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a permission MAY be granted to users of type <paramref name="userRoleId"/>,
    /// according to the <see cref="PermissionRestriction"/> rules.
    /// (Legacy: <c>YetkilendirmeController.YetkiKisitControl</c>)
    /// </summary>
    Task<bool> IsPermissionGrantableAsync(int permissionId, int userRoleId, CancellationToken ct = default);
}

/// <summary>
/// Effective permission calculation.
/// <para>
/// This is the single large LINQ query from the legacy
/// <c>ENSA_ISG.Algoritmalar.PermissionCheck.Authorize</c> moved into the domain. The rules are
/// applied in the same order as in the legacy code:
/// </para>
/// <list type="number">
///   <item><b>System administrator shortcut</b> — when <c>User.SystemAdministrator</c> (legacy
///         <c>SerAdmin</c>) is set, every permission is returned and no further check runs.</item>
///   <item><b>Subscription plan gate</b> — a permission that is NOT part of the organization's
///         <c>SubscriptionPlan</c> is dropped.
///         ("Bu eylem satın alınan paket dışı kalmaktadır...")</item>
///   <item><b>Organization type gate</b> — a permission that is not opened up to the
///         organization's <c>OrganizationType</c> is dropped.
///         ("Bu eylem belirlenen Kurum Türü içi kullanım dışı bırakılmıştır...")</item>
///   <item><b>Source union</b> — user type defaults ∪ permissions granted explicitly to the user.
///         If that union is empty the user is not authorized.</item>
///   <item><b>Explicit deny</b> — <c>UserPermission.Authorized == false</c> rows override
///         everything; deny always beats allow.</item>
///   <item><b>User type restriction</b> — permissions that violate the allow/deny list rule of
///         <see cref="Permission.PermissionRestrictionMode"/> are dropped.</item>
/// </list>
/// <para>
/// NOTE: steps 2 and 3 are GATES — a permission granted to the user individually still has no
/// effect if it cannot pass them. This is critical for preserving the legacy behaviour.
/// </para>
/// </summary>
public class PermissionManager : DomainService, IPermissionManager
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IReadOnlyRepository<UserType> _userTypeRepository;

    public PermissionManager(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IOrganizationRepository organizationRepository,
        IReadOnlyRepository<UserType> userRoleRepository)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _organizationRepository = organizationRepository;
        _userTypeRepository = userRoleRepository;
    }

    public async Task<List<Permission>> GetEffectivePermissionsAsync(int userId, CancellationToken ct = default)
    {
        // One query for the account, the person, the contract and the role assignments. These
        // used to be columns on User; they now live in the tables that own them, and asking each
        // of those separately would be several chances to ask the wrong one.
        var facts = await _userRepository.GetAuthorizationFactsAsync(userId, ct);
        if (facts is not { } who || !who.CanAct)
        {
            return [];
        }

        // 1) System administrator: every permission, no gate checks.
        if (who.IsSystemAdministrator)
        {
            return await _permissionRepository.GetListAsync(cancellationToken: ct);
        }

        var user = await _userRepository.FindAsync(userId, ct);
        if (user is null)
        {
            return [];
        }

        var candidateIds = await CalculateCandidatePermissionIdsAsync(user, ct);
        if (candidateIds.Count == 0)
        {
            return [];
        }

        var permissions = await _permissionRepository.GetByIdsAsync(candidateIds, ct);

        // 6) User type restriction — the restriction map is fetched in one query (no N+1).
        // The type comes from the employment link now. It used to be derived by taking the user's
        // StaffRole enum and searching UserType for a row carrying the same enum: the same fact in
        // two places, free to disagree.
        var userRoleId = who.UserTypeId;
        if (userRoleId is null)
        {
            // With an undefined user type only unrestricted ("everyone") permissions apply.
            return permissions
                .Where(y => y.PermissionRestrictionMode == PermissionRestrictionMode.Everyone)
                .ToList();
        }

        var restrictedIds = permissions
            .Where(y => y.PermissionRestrictionMode != PermissionRestrictionMode.Everyone)
            .Select(y => y.Id)
            .ToList();

        if (restrictedIds.Count == 0)
        {
            return permissions;
        }

        var restrictionMap = await _permissionRepository.GetPermissionRestrictionMapAsync(restrictedIds, ct);

        return permissions
            .Where(y => MatchesRestriction(y.PermissionRestrictionMode, restrictionMap, y.Id, userRoleId.Value))
            .ToList();
    }

    public async Task<HashSet<int>> GetEffectivePermissionIdsAsync(int userId, CancellationToken ct = default)
    {
        var permissions = await GetEffectivePermissionsAsync(userId, ct);
        return permissions.Select(y => y.Id).ToHashSet();
    }

    public async Task<bool> IsAuthorizedAsync(int userId, string permissionTarget, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(permissionTarget))
        {
            return false;
        }

        var permission = await _permissionRepository.FindByTargetAsync(permissionTarget, ct);
        if (permission is null)
        {
            // Legacy behaviour: an unknown target means "not released yet" => no access.
            return false;
        }

        var effectiveIds = await GetEffectivePermissionIdsAsync(userId, ct);
        return effectiveIds.Contains(permission.Id);
    }

    public async Task<List<string>> GetPermissionTargetsAsync(int userId, CancellationToken ct = default)
    {
        var permissions = await GetEffectivePermissionsAsync(userId, ct);
        return permissions
            .Select(y => y.PermissionTarget)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<bool> IsPermissionGrantableAsync(int permissionId, int userRoleId, CancellationToken ct = default)
    {
        var permission = await _permissionRepository.FindAsync(permissionId, ct);
        if (permission is null)
        {
            return false;
        }

        if (permission.PermissionRestrictionMode == PermissionRestrictionMode.Everyone)
        {
            return true;
        }

        var restrictedTypeIds = await _permissionRepository.GetPermissionRestrictionUserRoleIdsAsync(permissionId, ct);
        var isListed = restrictedTypeIds.Contains(userRoleId);

        return permission.PermissionRestrictionMode switch
        {
            PermissionRestrictionMode.OnlySelected => isListed,
            PermissionRestrictionMode.SelectedExcept => !isListed,
            _ => true
        };
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Produces the candidate permission id set with the gates (subscription plan + organization
    /// type) and the source union applied and explicit denials removed. The restriction check is
    /// NOT performed at this stage.
    /// </summary>
    private async Task<List<int>> CalculateCandidatePermissionIdsAsync(User user, CancellationToken ct)
    {
        if (user.TenantId is not int organizationId)
        {
            // A user not bound to a tenant (a host user) has no permissions unless they are a
            // system administrator.
            return [];
        }

        var organization = await _organizationRepository.FindAsync(organizationId, ct);
        if (organization is null || !organization.IsActive)
        {
            return [];
        }

        // 2) Subscription plan gate
        var packageIds = (await _permissionRepository.GetSubscriptionPlanPermissionIdsAsync(organization.SubscriptionPlanId, ct)).ToHashSet();
        if (packageIds.Count == 0)
        {
            return [];
        }

        // 3) Organization type gate
        var organizationTypeIds = (await _permissionRepository.GetOrganizationTypePermissionIdsAsync(organization.OrganizationTypeId, ct)).ToHashSet();
        if (organizationTypeIds.Count == 0)
        {
            return [];
        }

        // 4) Source union: user type defaults ∪ permissions granted explicitly to the user
        var sourceIds = new HashSet<int>();

        var userRoleId = await FindUserRoleIdAsync(user.StaffRole, ct);
        if (userRoleId is int typeId)
        {
            sourceIds.UnionWith(await _permissionRepository.GetUserRolePermissionIdsAsync(typeId, ct));
        }

        sourceIds.UnionWith(await _permissionRepository.GetUserPermissionPermissionIdsAsync(user.Id, ct));

        if (sourceIds.Count == 0)
        {
            return [];
        }

        // 5) Explicit deny — deny always wins.
        var redIds = await _permissionRepository.GetUserRedPermissionIdsAsync(user.Id, ct);
        sourceIds.ExceptWith(redIds);

        // What is left after the gates
        sourceIds.IntersectWith(packageIds);
        sourceIds.IntersectWith(organizationTypeIds);

        return [.. sourceIds];
    }

    /// <summary>
    /// Resolves the id of the <see cref="UserType"/> record for a <see cref="StaffRole"/> enum
    /// value. (In the legacy system this match was a string comparison between
    /// <c>User_T.StaffRole</c> and <c>UserType_T.UserTypeCode</c>.)
    /// </summary>
    private async Task<int?> FindUserRoleIdAsync(StaffRole staffRole, CancellationToken ct)
    {
        if (staffRole == StaffRole.Unspecified)
        {
            return null;
        }

        var userRole = await _userTypeRepository.FindAsync(
            kt => kt.StaffRole == staffRole && kt.IsActive,
            ct);

        return userRole?.Id;
    }

    /// <summary>Evaluates a single permission against its restriction rule.</summary>
    private static bool MatchesRestriction(
        PermissionRestrictionMode target,
        Dictionary<int, List<int>> restrictionMap,
        int permissionId,
        int userRoleId)
    {
        if (target == PermissionRestrictionMode.Everyone)
        {
            return true;
        }

        var isListed = restrictionMap.TryGetValue(permissionId, out var typeIds) && typeIds.Contains(userRoleId);

        return target switch
        {
            // Allow list: only the listed types can receive the permission.
            PermissionRestrictionMode.OnlySelected => isListed,
            // Deny list: the listed types cannot receive the permission.
            PermissionRestrictionMode.SelectedExcept => !isListed,
            _ => true
        };
    }
}
