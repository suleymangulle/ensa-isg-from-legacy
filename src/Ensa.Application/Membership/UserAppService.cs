using Ensa.Domain.Repositories;
using Ensa.Domain.Tenancy;
using System.Globalization;
using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Membership.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Membership;

/// <summary>
/// Administrative user management.
/// <para>
/// <b>Reads</b> go through <see cref="IUserRepository"/> (paging, filtering and the combined
/// navigation view live there). <b>Writes</b> go through
/// <see cref="UserManager{TUser}"/> without exception, because <see cref="User"/> derives from
/// <c>IdentityUser&lt;int&gt;</c>: the manager owns the password hashing, the normalized lookup
/// keys, the security stamp and the role join table. Writing those through a repository would
/// produce a row that Identity can no longer authenticate.
/// </para>
/// <para>
/// The manager persists on its own — after <c>CreateAsync</c>, <c>UpdateAsync</c> or
/// <c>DeleteAsync</c> the row is already saved, so no repository call may follow.
/// </para>
/// </summary>
public class UserAppService(
    IServiceProvider serviceProvider,
    IUserRepository userRepository,
    IPermissionManager permissionManager,
    UserManager<User> userManager,
    IRepository<UserProfile> userProfileRepository,
    IRepository<UserEmployment> userEmploymentRepository,
    IReadOnlyRepository<UserType> userTypeRepository,
    IReadOnlyRepository<Organization> organizationRepository)
    : EnsaAppService(serviceProvider), IUserAppService
{
    /// <summary>Maximum number of records returned by the drop-down endpoint.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<UserDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.User.Default);

        var user = await userRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(User), id);

        return ObjectMapper.Map<User, UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.User.Default);

        var navigation = await userRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(User), id);

        // The effective permission set is owned by IPermissionManager (subscription-plan and
        // organization-type gates, staff-role defaults, explicit denials). It is never
        // recomputed here; the repository result is used only when it already carries it.
        var permissions = navigation.Permissions.Count > 0
            ? navigation.Permissions
            : await permissionManager.GetEffectivePermissionsAsync(id, cancellationToken);

        return new UserNavigationDto
        {
            User = ObjectMapper.Map<User, UserDto>(navigation.User),
            Organization = Lookup(navigation.Organization?.Id, navigation.Organization?.Name),
            Office = Lookup(navigation.Office?.Id, navigation.Office?.Name),
            Offices = [.. navigation.Offices.Select(o => new LookupDto
            {
                Id = o.Id,
                DisplayName = o.Name,
                IsActive = o.IsActive
            })],
            OfficeAssignments = [.. navigation.OfficeAssignments.Select(a => new UserOfficeAssignmentDto
            {
                Id = a.Id,
                OfficeId = a.OfficeId,
                OfficeName = navigation.Offices.Find(o => o.Id == a.OfficeId)?.Name ?? string.Empty,
                MonthlyWorkDurationMinutes = a.MonthlyWorkDurationMinutes
            })],
            Roles = [.. navigation.Roles.Select(r => new LookupDto
            {
                Id = r.Id,
                DisplayName = r.Name ?? string.Empty,
                Code = r.NormalizedName
            })],
            Permissions = ObjectMapper.Map<List<Permission>, List<PermissionDto>>(permissions),
            UserType = Lookup(navigation.UserType?.Id, navigation.UserType?.Name),
            StaffRole = navigation.User.StaffRole,
            City = Lookup(navigation.User.CityId, navigation.CityName),
            District = Lookup(navigation.User.DistrictId, navigation.DistrictName),
            OrganizationIds = [.. navigation.OrganizationIds],
            PhotoSizeBytes = navigation.PhotoDocumentBoyutu
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<UserListDto>> GetListAsync(
        GetUserListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.User.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "Name ASC");

        var total = await userRepository.GetCountAsync(predicate, cancellationToken);

        var records = await userRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<User>, List<UserListDto>>(records);

        return new PagedResultDto<UserListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.User.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        // The names and the active flag live on the profile now, so the search runs there and
        // brings the account along rather than the other way round.
        var profiles = await userProfileRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "Name ASC",
            predicate: p => p.IsActive
                            && (search == null
                                || p.Name.Contains(search)
                                || p.LastName.Contains(search)),
            cancellationToken);

        var userIds = profiles.Select(p => p.UserId).ToList();

        var accounts = await userRepository.GetListAsync(
            u => userIds.Contains(u.Id), cancellationToken);

        var result = profiles
            .Select(profile =>
            {
                var account = accounts.Find(u => u.Id == profile.UserId);

                return new LookupDto
                {
                    Id = profile.UserId,
                    DisplayName = account is null
                        ? $"{profile.Name} {profile.LastName}".Trim()
                        : account.GetDisplayName(profile),
                    Code = account?.UserName,
                    IsActive = profile.IsActive
                };
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateAsync(
        CreateUserDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.User.Create);

        await EnsureNationalIdIsFreeAsync(input.NationalId, exceptUserId: null, cancellationToken);

        var user = ObjectMapper.Map<CreateUserDto, User>(input);

        // Which organization the user joins.
        //
        // A caller inside an organization can only ever create users in that organization: the
        // value from the request is ignored and the DbContext stamps the ambient tenant. A host
        // caller has no ambient tenant, so it states the organization explicitly — that is the
        // only way an administrator who manages every organization can create a user inside one.
        if (CurrentTenant.Id is null)
        {
            if (input.TenantId is { } targetTenantId)
            {
                _ = await organizationRepository.FindAsync(targetTenantId, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Organization), targetTenantId);

                user.TenantId = targetTenantId;
            }
        }
        else
        {
            user.TenantId = CurrentTenant.Id;
        }

        // UserManager hashes the password, stamps the security token, normalizes the lookup
        // keys AND saves the row. No repository insert may follow this call.
        EnsureIdentitySucceeded(await userManager.CreateAsync(user, input.Password));

        if (input.Roles.Length > 0)
        {
            EnsureIdentitySucceeded(await userManager.AddToRolesAsync(user, NormalizeRoles(input.Roles)));
        }

        await CreatePersonAsync(user, input, cancellationToken);

        Logger.LogInformation("User created: {UserId} - {UserName}", user.Id, user.UserName);

        return ObjectMapper.Map<User, UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateAsync(
        int id,
        UpdateUserDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.User.Update);

        var user = await FindTrackedUserAsync(id);

        await EnsureNationalIdIsFreeAsync(input.NationalId, exceptUserId: id, cancellationToken);

        // Captured before the map, which overwrites Email in place - comparing afterwards
        // would always report "unchanged" and leave NormalizedEmail stale.
        var previousEmail = user.Email;

        // The national id is an encrypted, uniquely indexed statutory identifier and it is
        // deliberately NOT returned by UserDto, so no caller can read it back. A client editing
        // a phone number therefore has nothing to send here, and an absolute map would erase the
        // stored value. An omitted national id means "keep the current one", exactly as an
        // omitted password does elsewhere; clearing one is a deliberate act, not a side effect.
        var profile = await userProfileRepository.FindAsync(p => p.UserId == id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(UserProfile), id);

        var employment = await userEmploymentRepository.FindAsync(e => e.UserId == id, cancellationToken);

        ObjectMapper.Map(input, user);

        profile.Name = input.Name;
        profile.LastName = input.LastName;
        profile.Address = input.Address;
        profile.CityId = input.CityId;
        profile.DistrictId = input.DistrictId;
        profile.PhotoDocumentId = input.PhotoDocumentId;
        profile.Color = input.Color;
        profile.IsActive = input.IsActive;

        // An empty identity number means "leave it alone" rather than "clear it": the field is
        // encrypted and write-only on the form, so a blank one is a form that did not send it.
        if (!string.IsNullOrWhiteSpace(input.NationalId))
        {
            profile.NationalId = input.NationalId;
        }

        if (employment is not null)
        {
            employment.HireDate = input.HireDate;
            employment.TerminationDate = input.TerminationDate;
            employment.GrossSalary = input.GrossSalary;
            employment.PartTime = input.PartTime;
        }

        await userProfileRepository.UpdateAsync(profile, autoSave: true, cancellationToken);

        if (employment is not null)
        {
            await userEmploymentRepository.UpdateAsync(employment, autoSave: true, cancellationToken);
        }

        // The e-mail is a normalized lookup key, so it goes through the manager rather than
        // being left to the plain property assignment done by the mapper.
        if (!string.Equals(previousEmail, input.Email, StringComparison.OrdinalIgnoreCase))
        {
            EnsureIdentitySucceeded(await userManager.SetEmailAsync(user, input.Email));
        }

        EnsureIdentitySucceeded(await userManager.UpdateAsync(user));

        Logger.LogInformation("User updated: {UserId}", id);

        return ObjectMapper.Map<User, UserDto>(user);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.User.Delete);

        var user = await FindTrackedUserAsync(id);

        if (CurrentUser.Id == id)
        {
            throw new BusinessException(
                    "You cannot delete your own account.",
                    "Ensa:User:CannotDeleteSelf")
                .WithData("UserName", user.UserName);
        }

        // User implements ISoftDelete, so the DbContext turns this physical delete into a
        // logical one; the row stays available for the audit trail.
        EnsureIdentitySucceeded(await userManager.DeleteAsync(user));

        Logger.LogInformation("User deleted: {UserId}", id);
    }

    /// <inheritdoc />
    public async Task ResetPasswordAsync(
        int id,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
        await CheckPermissionAsync(EnsaPermissions.User.Update);

        var user = await FindTrackedUserAsync(id);

        // An administrative reset does not know the current password, so a self-issued reset
        // token is used instead of ChangePasswordAsync.
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        EnsureIdentitySucceeded(await userManager.ResetPasswordAsync(user, token, newPassword));

        // Force a fresh sign-in: rotating the stamp invalidates every outstanding refresh token.
        EnsureIdentitySucceeded(await userManager.UpdateSecurityStampAsync(user));

        // The flag is on the profile now. Somebody else has just chosen this password, so the
        // owner has to replace it before doing anything else.
        var profile = await userProfileRepository.FindAsync(p => p.UserId == id, cancellationToken);

        if (profile is not null)
        {
            profile.MustChangePassword = true;
            await userProfileRepository.UpdateAsync(profile, autoSave: true, cancellationToken);
        }

        Logger.LogInformation("Password reset for user {UserId} by {ActorId}.", id, CurrentUser.Id);
    }

    /// <inheritdoc />
    public async Task AssignRolesAsync(
        int id,
        string[] roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roles);
        await CheckPermissionAsync(EnsaPermissions.User.Update);

        var user = await FindTrackedUserAsync(id);

        var requested = NormalizeRoles(roles);
        var current = await userManager.GetRolesAsync(user);

        var removed = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToList();
        var added = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToList();

        if (removed.Count > 0)
        {
            EnsureIdentitySucceeded(await userManager.RemoveFromRolesAsync(user, removed));
        }

        if (added.Count > 0)
        {
            // An unknown role name comes back as a "RoleNotFound" IdentityError and is
            // surfaced as a field-level validation failure.
            EnsureIdentitySucceeded(await userManager.AddToRolesAsync(user, added));
        }

        // Role membership feeds the permission claims baked into the access token.
        EnsureIdentitySucceeded(await userManager.UpdateSecurityStampAsync(user));

        Logger.LogInformation(
            "Roles of user {UserId} updated. Added={Added}, Removed={Removed}",
            id, added.Count, removed.Count);
    }

    /// <inheritdoc />
    public async Task SetActiveStateAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.User.Update);

        var user = await FindTrackedUserAsync(id);

        if (!isActive && CurrentUser.Id == id)
        {
            throw new BusinessException(
                    "You cannot deactivate your own account.",
                    "Ensa:User:CannotDeactivateSelf")
                .WithData("UserName", user.UserName);
        }

        var profile = await userProfileRepository.FindAsync(p => p.UserId == id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(UserProfile), id);

        if (profile.IsActive == isActive)
        {
            return;
        }

        profile.IsActive = isActive;
        await userProfileRepository.UpdateAsync(profile, autoSave: true, cancellationToken);

        if (!isActive)
        {
            // A deactivated user must lose its live sessions immediately.
            EnsureIdentitySucceeded(await userManager.UpdateSecurityStampAsync(user));
        }

        Logger.LogInformation("Active state of user {UserId} set to {IsActive}.", id, isActive);
    }

    // ----------------------------------------------------------- internals

    /// <summary>
    /// Loads the user through <see cref="UserManager{TUser}"/> so that the returned instance is
    /// tracked by the same context the manager writes through.
    /// </summary>
    private async Task<User> FindTrackedUserAsync(int id)
        => await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture))
           ?? throw new EntityNotFoundException(typeof(User), id);

    private async Task EnsureNationalIdIsFreeAsync(
        string? nationalId,
        int? exceptUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return;
        }

        var exists = await userRepository.NationalIdExistsAsync(
            nationalId.Trim(),
            exceptUserId,
            cancellationToken);

        if (exists)
        {
            throw new BusinessException(
                    "Another user with this national id is already registered.",
                    "Ensa:User:NationalIdAlreadyRegistered")
                .WithData("NationalId", nationalId.Trim());
        }
    }

    /// <summary>
    /// Turns a failed <see cref="IdentityResult"/> into a field-level validation exception so
    /// the client can highlight the offending input instead of showing a generic message.
    /// </summary>
    /// <summary>
    /// Gives a new account the person and the contract that go with it.
    /// <para>
    /// <b>Why this is not optional.</b> Authorization reads whether a user is active from
    /// <see cref="UserProfile"/>, and a user with no profile row is treated as inactive — correctly,
    /// because a half-created account should not be able to act. So an account created without one
    /// can sign in and then find it may do nothing at all, which is a confusing way to fail.
    /// </para>
    /// <para>
    /// Both rows are written after Identity has accepted the account, because until then there is
    /// no id to point them at.
    /// </para>
    /// </summary>
    private async Task CreatePersonAsync(
        User user,
        CreateUserDto input,
        CancellationToken cancellationToken)
    {
        await userProfileRepository.InsertAsync(new UserProfile
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Name = input.Name,
            LastName = input.LastName,
            NationalId = input.NationalId,
            Address = input.Address,
            CityId = input.CityId,
            DistrictId = input.DistrictId,
            Color = input.Color,
            PhotoDocumentId = input.PhotoDocumentId,
            IsActive = input.IsActive,

            // A new account is given a password by whoever created it, so the first thing its
            // owner should do is replace it with one only they know.
            MustChangePassword = true,
        }, autoSave: true, cancellationToken);

        // The request still names a staff role, so the type is resolved from it here. The link is
        // what everything reads now -- authorization, the menu and the navigation view all ask
        // UserEmployment.UserTypeId -- so leaving it null would create an account that can sign in
        // and then do nothing, which is how the first version of this failed.
        var userTypeId = input.StaffRole == StaffRole.Unspecified
            ? null
            : (await userTypeRepository.FindAsync(
                t => t.StaffRole == input.StaffRole && t.IsActive, cancellationToken))?.Id;

        await userEmploymentRepository.InsertAsync(new UserEmployment
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            UserTypeId = userTypeId,
            HireDate = input.HireDate,
            TerminationDate = input.TerminationDate,
            GrossSalary = input.GrossSalary,
            PartTime = input.PartTime,
        }, autoSave: true, cancellationToken);
    }

    private static void EnsureIdentitySucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new EnsaValidationException(
            [.. result.Errors.Select(e => new ValidationError(MapMember(e.Code), e.Description))]);
    }

    /// <summary>Maps an Identity error code onto the input field it belongs to.</summary>
    private static string MapMember(string identityErrorCode)
    {
        if (identityErrorCode.Contains("Password", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(CreateUserDto.Password);
        }

        if (identityErrorCode.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(CreateUserDto.UserName);
        }

        if (identityErrorCode.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(CreateUserDto.Email);
        }

        return identityErrorCode.Contains("Role", StringComparison.OrdinalIgnoreCase)
            ? nameof(CreateUserDto.Roles)
            : string.Empty;
    }

    /// <summary>Trims, drops blanks and removes duplicates from a requested role name list.</summary>
    private static List<string> NormalizeRoles(IEnumerable<string> roles)
        => [.. roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static LookupDto? Lookup(int? id, string? name)
        => id is null ? null : new LookupDto { Id = id.Value, DisplayName = name ?? string.Empty };

    /// <summary>
    /// Builds the list predicate. Every clause is guarded by a captured local, which the
    /// query provider folds away when the corresponding filter was not supplied - so a single
    /// expression covers all filter combinations without an expression-tree rewriter.
    /// </summary>
    private static Expression<Func<User, bool>> BuildFilter(GetUserListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var staffRole = input.StaffRole;
        var officeId = input.OfficeId;
        var companyId = input.CompanyId;
        var isActive = input.IsActive;

        return u =>
            (search == null
             || u.Name.Contains(search)
             || u.LastName.Contains(search)
             || (u.UserName != null && u.UserName.Contains(search))
             || (u.Email != null && u.Email.Contains(search))
             || (u.Gsm != null && u.Gsm.Contains(search)))
            && (staffRole == null || u.StaffRole == staffRole)
            && (officeId == null || u.OfficeId == officeId)
            && (companyId == null || u.CompanyId == companyId)
            && (isActive == null || u.IsActive == isActive);
    }
}
