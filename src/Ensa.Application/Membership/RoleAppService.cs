using System.Globalization;
using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Membership;

/// <summary>
/// Role management.
/// <para>
/// Reads are served from <see cref="IRepository{TEntity}"/> because they need paging,
/// filtering and sorting. Every write goes through <see cref="RoleManager{TRole}"/>: it owns
/// the normalized name and the concurrency stamp, and a role whose normalized name does not
/// match its name is invisible to <c>UserManager.AddToRoleAsync</c>.
/// </para>
/// <para>
/// The manager saves the row itself, so no repository write follows a manager call.
/// </para>
/// </summary>
public class RoleAppService(
    IServiceProvider serviceProvider,
    IRepository<Role> roleRepository,
    RoleManager<Role> roleManager,
    UserManager<User> userManager,
    IRepository<RoleProfile> roleProfileRepository)
    : EnsaAppService(serviceProvider), IRoleAppService
{
    /// <summary>Maximum number of records returned by the drop-down endpoint.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<RoleDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Role.Default);

        var role = await roleRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Role), id);

        var dto = ObjectMapper.Map<Role, RoleDto>(role);

        if (!string.IsNullOrWhiteSpace(role.Name))
        {
            var members = await userManager.GetUsersInRoleAsync(role.Name);
            dto.UserCount = members.Count;
        }

        return dto;
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<RoleListDto>> GetListAsync(
        GetRoleListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Role.Default);

        var predicate = await BuildFilterAsync(input, cancellationToken);
        var sorting = NormalizeSorting(input.Sorting, "Name ASC");

        var total = await roleRepository.GetCountAsync(predicate, cancellationToken);

        var records = await roleRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Role>, List<RoleListDto>>(records);

        return new PagedResultDto<RoleListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Role.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var records = await roleRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "Name ASC",
            predicate: r => search == null || (r.Name != null && r.Name.Contains(search)),
            cancellationToken);

        var result = records
            .Select(r => new LookupDto
            {
                Id = r.Id,
                DisplayName = r.Name ?? string.Empty,
                Code = r.NormalizedName
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<RoleDto> CreateAsync(
        CreateRoleDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Role.Create);

        var role = ObjectMapper.Map<CreateRoleDto, Role>(input);
        role.Name = input.Name.Trim();

        // RoleManager validates uniqueness, fills NormalizedName AND saves the row.
        // Calling the repository afterwards would insert the same role twice.
        EnsureIdentitySucceeded(await roleManager.CreateAsync(role));

        Logger.LogInformation("Role created: {RoleId} - {RoleName}", role.Id, role.Name);

        return ObjectMapper.Map<Role, RoleDto>(role);
    }

    /// <inheritdoc />
    public async Task<RoleDto> UpdateAsync(
        int id,
        UpdateRoleDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Role.Update);

        var role = await FindTrackedRoleAsync(id);

        var newName = input.Name.Trim();
        var renamed = !string.Equals(role.Name, newName, StringComparison.Ordinal);

        // A static role is referenced by name from seed data, authorization policies and the
        // permission catalogue, so renaming it would silently break those references.
        // Its description and default flag remain editable.
        var profile = await roleProfileRepository.FindAsync(p => p.RoleId == id, cancellationToken);

        if (renamed && (profile?.IsStatic ?? false))
        {
            throw new BusinessException(
                    "System roles cannot be renamed.",
                    "Ensa:Role:SystemRoleImmutable")
                .WithData("RoleName", role.Name);
        }

        ObjectMapper.Map(input, role);

        // Assigned after the map so the stored name is always the trimmed one.
        role.Name = newName;

        EnsureIdentitySucceeded(await roleManager.UpdateAsync(role));

        Logger.LogInformation("Role updated: {RoleId}", id);

        return ObjectMapper.Map<Role, RoleDto>(role);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Role.Delete);

        var role = await FindTrackedRoleAsync(id);

        var profile = await roleProfileRepository.FindAsync(p => p.RoleId == id, cancellationToken);

        if (profile?.IsStatic ?? false)
        {
            throw new BusinessException(
                    "System roles cannot be deleted.",
                    "Ensa:Role:SystemRoleImmutable")
                .WithData("RoleName", role.Name);
        }

        EnsureIdentitySucceeded(await roleManager.DeleteAsync(role));

        Logger.LogInformation("Role deleted: {RoleId}", id);
    }

    // ----------------------------------------------------------- internals

    /// <summary>
    /// Loads the role through <see cref="RoleManager{TRole}"/> so the instance is tracked by
    /// the same context the manager writes through.
    /// </summary>
    private async Task<Role> FindTrackedRoleAsync(int id)
        => await roleManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture))
           ?? throw new EntityNotFoundException(typeof(Role), id);

    private static void EnsureIdentitySucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new EnsaValidationException(
            [.. result.Errors.Select(e => new ValidationError(nameof(CreateRoleDto.Name), e.Description))]);
    }

    /// <summary>
    /// The name is Identity's; the description and the two flags are ours and live in
    /// <see cref="RoleProfile"/>. The filter therefore takes the ids the profile side matched and
    /// narrows the roles to those, rather than pretending one predicate can see both tables.
    /// </summary>
    private async Task<Expression<Func<Role, bool>>> BuildFilterAsync(
        GetRoleListInput input,
        CancellationToken cancellationToken)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var isStatic = input.IsStatic;
        var isDefault = input.IsDefault;

        if (search is null && isStatic is null && isDefault is null)
        {
            return _ => true;
        }

        var profiles = await roleProfileRepository.GetListAsync(
            p => (isStatic == null || p.IsStatic == isStatic)
                 && (isDefault == null || p.IsDefault == isDefault)
                 && (search == null || (p.Description != null && p.Description.Contains(search))),
            cancellationToken);

        var matchedByProfile = profiles.Select(p => p.RoleId).ToList();

        // A search matches either side: a role whose name contains the text, or one whose
        // description does. A flag filter only ever matches through the profile.
        if (search is not null && isStatic is null && isDefault is null)
        {
            return r => (r.Name != null && r.Name.Contains(search)) || matchedByProfile.Contains(r.Id);
        }

        return r => matchedByProfile.Contains(r.Id);
    }
}
