using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Membership.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Membership;

/// <summary>
/// Read access to the permission catalogue plus per-user permission assignment.
/// <para>
/// The effective-permission algorithm is <b>not</b> reimplemented here. It lives in
/// <see cref="IPermissionManager"/> (subscription-plan gate, organization-type gate,
/// staff-role defaults union explicit grants, explicit denials, staff-role restriction
/// modes) and this service only calls into it. Duplicating any part of that ordering would
/// let the API and the token issuer disagree about what a user may do.
/// </para>
/// <para>
/// <see cref="Permission"/> is a host catalogue table seeded from the <c>EnsaPermissions</c>
/// constants, so no create or delete operation is exposed - only reads and user assignment.
/// </para>
/// </summary>
public class PermissionAppService(
    IServiceProvider serviceProvider,
    IPermissionRepository permissionRepository,
    IPermissionManager permissionManager,
    IUserRepository userRepository,
    IRepository<UserPermission> userPermissionRepository,
    IRepository<UserTypePermission> userTypePermissionRepository,
    IReadOnlyRepository<UserType> userTypeRepository)
    : EnsaAppService(serviceProvider), IPermissionAppService
{
    /// <inheritdoc />
    public async Task<PagedResultDto<PermissionDto>> GetListAsync(
        GetPermissionListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Permission.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "SortOrder ASC");

        var total = await permissionRepository.GetCountAsync(predicate, cancellationToken);

        var records = await permissionRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Permission>, List<PermissionDto>>(records);

        return new PagedResultDto<PermissionDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<PermissionTreeDto> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Permission.Default);

        var permissions = await permissionRepository.GetListAsync(cancellationToken: cancellationToken);

        // The catalogue is small and bounded (one row per EnsaPermissions constant), so the
        // whole set is fetched once and assembled in memory instead of issuing one query per
        // hierarchy level.
        var nodes = permissions.ToDictionary(
            p => p.Id,
            p => new PermissionTreeNodeDto
            {
                Id = p.Id,
                ParentPermissionId = p.ParentPermissionId,
                PermissionType = p.PermissionType,
                PermissionTarget = p.PermissionTarget,
                PermissionName = p.PermissionName,
                PermissionDescription = p.PermissionDescription,
                PermissionRestrictionMode = p.PermissionRestrictionMode,
                SortOrder = p.SortOrder
            });

        var roots = new List<PermissionTreeNodeDto>();

        foreach (var node in nodes.Values)
        {
            // A node whose parent is missing (or points outside the catalogue) is treated as a
            // root so that a broken parent link can never make a permission disappear.
            if (node.ParentPermissionId is { } parentId && nodes.TryGetValue(parentId, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        SortRecursively(roots);

        return new PermissionTreeDto
        {
            Roots = roots,
            TotalCount = nodes.Count
        };
    }

    /// <inheritdoc />
    public async Task<UserPermissionsDto> GetUserPermissionsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Permission.Default);

        var user = await userRepository.FindAsync(userId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(User), userId);

        // Authoritative answer - computed by the domain service, never here.
        var effective = await permissionManager.GetEffectivePermissionsAsync(userId, cancellationToken);

        var granted = await permissionRepository.GetUserPermissionPermissionIdsAsync(userId, cancellationToken);
        var denied = await permissionRepository.GetUserRedPermissionIdsAsync(userId, cancellationToken);

        return new UserPermissionsDto
        {
            UserId = userId,
            EffectivePermissions = ObjectMapper.Map<List<Permission>, List<PermissionDto>>(effective),
            GrantedPermissionIds = granted,
            DeniedPermissionIds = denied,
            SystemAdministrator = user.SystemAdministrator
        };
    }

    /// <inheritdoc />
    public async Task SaveUserPermissionsAsync(
        int userId,
        UpdateUserPermissionsDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Permission.Update);

        _ = await userRepository.FindAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(User), userId);

        var granted = Distinct(input.GrantedPermissionIds);
        var denied = Distinct(input.DeniedPermissionIds);

        // A permission that is both granted and denied is ambiguous on its face. The engine
        // would silently apply the denial; rejecting the payload instead keeps the stored
        // overrides an honest record of what the administrator asked for.
        var conflicting = granted.Intersect(denied).ToList();
        if (conflicting.Count > 0)
        {
            throw new BusinessException(
                    "A permission cannot be granted and denied at the same time.",
                    "Ensa:Permission:ConflictingOverride")
                .WithData("PermissionId", conflicting[0]);
        }

        await EnsurePermissionsExistAsync([.. granted, .. denied], cancellationToken);

        // Replace, not merge: both lists are absolute. Existing rows are removed and saved
        // first so a unique (UserId, PermissionId) index cannot collide with the new rows
        // inside a single SaveChanges.
        var existing = await userPermissionRepository.GetListAsync(
            up => up.UserId == userId,
            cancellationToken);

        if (existing.Count > 0)
        {
            await userPermissionRepository.DeleteManyAsync(existing, autoSave: true, cancellationToken);
        }

        var rows = new List<UserPermission>(granted.Count + denied.Count);

        rows.AddRange(granted.Select(permissionId => new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            Authorized = true,
            IsActive = true
        }));

        rows.AddRange(denied.Select(permissionId => new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            Authorized = false,
            IsActive = true
        }));

        if (rows.Count > 0)
        {
            await userPermissionRepository.InsertManyAsync(rows, autoSave: true, cancellationToken);
        }

        Logger.LogInformation(
            "Permission overrides of user {UserId} replaced. Granted={Granted}, Denied={Denied}",
            userId, granted.Count, denied.Count);
    }

    // ----------------------------------------------------------- internals

    private async Task EnsurePermissionsExistAsync(
        List<int> permissionIds,
        CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
        {
            return;
        }

        var found = await permissionRepository.GetByIdsAsync(permissionIds, cancellationToken);
        var foundIds = found.Select(p => p.Id).ToHashSet();

        var missing = permissionIds.Find(id => !foundIds.Contains(id));
        if (missing != default)
        {
            throw new BusinessException(
                    "The requested permission is not defined in the catalogue.",
                    "Ensa:Permission:UnknownPermission")
                .WithData("PermissionId", missing);
        }
    }

    /// <summary>Orders every level of the tree by sort order, then by display name.</summary>
    private static void SortRecursively(List<PermissionTreeNodeDto> nodes)
    {
        nodes.Sort(static (left, right) => left.SortOrder != right.SortOrder
            ? left.SortOrder.CompareTo(right.SortOrder)
            : string.Compare(left.PermissionName, right.PermissionName, StringComparison.Ordinal));

        foreach (var node in nodes)
        {
            SortRecursively(node.Children);
        }
    }

    private static List<int> Distinct(int[] ids)
        => [.. ids.Where(id => id > 0).Distinct()];

    private static Expression<Func<Permission, bool>> BuildFilter(GetPermissionListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var permissionType = input.PermissionType;
        var parentPermissionId = input.ParentPermissionId;

        return p =>
            (search == null
             || p.PermissionName.Contains(search)
             || p.PermissionTarget.Contains(search))
            && (permissionType == null || p.PermissionType == permissionType)
            && (parentPermissionId == null || p.ParentPermissionId == parentPermissionId);
    }
    /// <inheritdoc />
    public async Task<UserTypePermissionsDto> GetUserTypePermissionsAsync(
        int userTypeId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Permission.Default);

        var userType = await userTypeRepository.FindAsync(userTypeId, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(UserType), userTypeId);

        var assignments = await userTypePermissionRepository.GetListAsync(
            row => row.UserTypeId == userTypeId && row.IsActive,
            cancellationToken);

        var permissionIds = assignments.Select(row => row.PermissionId).Distinct().ToList();

        // One batched query, not one per assignment.
        var permissions = permissionIds.Count == 0
            ? []
            : await permissionRepository.GetListAsync(
                permission => permissionIds.Contains(permission.Id),
                cancellationToken);

        return new UserTypePermissionsDto
        {
            UserTypeId = userTypeId,
            UserTypeName = userType.Name,
            PermissionIds = permissionIds,
            Permissions = ObjectMapper.Map<List<Permission>, List<PermissionDto>>(
                [.. permissions.OrderBy(permission => permission.PermissionTarget, StringComparer.Ordinal)]),
        };
    }

    /// <inheritdoc />
    public async Task SaveUserTypePermissionsAsync(
        int userTypeId,
        UpdateUserTypePermissionsDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Permission.Update);

        _ = await userTypeRepository.FindAsync(userTypeId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(UserType), userTypeId);

        var permissionIds = Distinct(input.PermissionIds);

        await EnsurePermissionsExistAsync(permissionIds, cancellationToken);

        // A permission restricted away from this staff type would be stored and then silently
        // dropped when the effective set is computed, leaving the screen claiming something the
        // system does not honour. Refusing is the honest answer.
        foreach (var permissionId in permissionIds)
        {
            if (!await permissionManager.IsPermissionGrantableAsync(permissionId, userTypeId, cancellationToken))
            {
                throw new BusinessException(
                        "This permission cannot be granted to this staff type.",
                        "Ensa:Permission:NotGrantableToUserType")
                    .WithData("PermissionId", permissionId)
                    .WithData("UserTypeId", userTypeId);
            }
        }

        // Replace, not merge: the list is absolute. Existing rows are removed and saved first so
        // a unique (UserTypeId, PermissionId) index cannot collide inside a single SaveChanges.
        var existing = await userTypePermissionRepository.GetListAsync(
            row => row.UserTypeId == userTypeId,
            cancellationToken);

        if (existing.Count > 0)
        {
            await userTypePermissionRepository.DeleteManyAsync(existing, autoSave: true, cancellationToken);
        }

        if (permissionIds.Count > 0)
        {
            await userTypePermissionRepository.InsertManyAsync(
                permissionIds.Select(permissionId => new UserTypePermission
                {
                    UserTypeId = userTypeId,
                    PermissionId = permissionId,
                    IsActive = true,
                }),
                autoSave: true,
                cancellationToken);
        }

        Logger.LogInformation(
            "Staff type permissions replaced. UserTypeId={UserTypeId}, PermissionCount={PermissionCount}",
            userTypeId, permissionIds.Count);
    }
}
