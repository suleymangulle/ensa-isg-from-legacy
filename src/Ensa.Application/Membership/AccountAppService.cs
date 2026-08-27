using System.Globalization;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Domain.Membership;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Membership;

/// <summary>
/// Operations the signed-in user performs on their own account.
/// Issuing tokens is the responsibility of <c>AuthorizationController</c> (OpenIddict) in the host
/// layer.
/// </summary>
public class AccountAppService(
    IServiceProvider serviceProvider,
    UserManager<User> userManager,
    IPermissionResolver permissionResolver,
    IUserRepository userRepository,
    IReadOnlyRepository<UserProfile> userProfileRepository,
    IReadOnlyRepository<UserEmployment> userEmploymentRepository,
    IReadOnlyRepository<UserOffice> userOfficeRepository,
    IReadOnlyRepository<UserType> userTypeRepository)
    : EnsaAppService(serviceProvider), IAccountAppService
{
    /// <inheritdoc />
    public async Task<ProfileDto> GetProfileAsync()
    {
        var user = await GetCurrentUserAsync();
        var roles = await userManager.GetRolesAsync(user);

        // The person, the contract and the office. A profile is written when an account is
        // created, so a user without one is a record from before the split; the screen shows
        // what it can rather than failing on it.
        var profile = await userProfileRepository.FindAsync(p => p.UserId == user.Id);
        var employment = await userEmploymentRepository.FindAsync(e => e.UserId == user.Id);
        var office = await userOfficeRepository.FindAsync(o => o.UserId == user.Id);

        // Through the type the employment points at, rather than a copy of that type's own
        // enum kept on the user row beside it.
        var staffRole = employment?.UserTypeId is int typeId
            ? (await userTypeRepository.FindAsync(t => t.Id == typeId))?.StaffRole
              ?? StaffRole.Unspecified
            : StaffRole.Unspecified;

        return new ProfileDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Name = profile?.Name ?? string.Empty,
            LastName = profile?.LastName ?? string.Empty,
            FullName = user.GetDisplayName(profile),
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            Gsm = user.PhoneNumber,
            PhotoDocumentId = profile?.PhotoDocumentId,
            Color = profile?.Color,
            TenantId = user.TenantId,
            OfficeId = office?.OfficeId,
            CompanyId = profile?.CompanyId,
            StaffRole = staffRole,
            SystemAdministrator = roles.Contains(EnsaRoleNames.SystemAdministrator),
            OrganizationAdmin = roles.Contains(EnsaRoleNames.OrganizationAdministrator),
            OfficeAdmin = roles.Contains(EnsaRoleNames.OfficeAdministrator),
            Roles = [.. roles],
            MustChangePassword = profile?.MustChangePassword ?? false,
            ContractApproved = profile?.ContractApproved ?? false,
            LockoutEnd = user.LockoutEnd
        };
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(ChangePasswordDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var user = await GetCurrentUserAsync();

        var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            throw new EnsaValidationException(
                [.. result.Errors.Select(e => new ValidationError(MapMember(e.Code), e.Description))]);
        }

        // Invalidate the outstanding refresh tokens once a new password has been set.
        await userManager.UpdateSecurityStampAsync(user);

        Logger.LogInformation("User {UserId} changed their password.", user.Id);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<string>> GetPermissionsAsync()
    {
        var user = await GetCurrentUserAsync();
        var permissions = await permissionResolver.GetPermissionsAsync(user);

        return new ListResultDto<string>(permissions);
    }

    // -------------------------------------------------------------- helpers

    private async Task<User> GetCurrentUserAsync()
    {
        var userId = GetRequiredUserId();

        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture))
                   ?? throw new EntityNotFoundException(typeof(User), userId);

        var facts = await userRepository.GetAuthorizationFactsAsync(user.Id);

        if (facts is not { } who || !who.CanSignIn())
        {
            throw new EnsaAuthorizationException("Your account is not active.", "Ensa:UserNotActive");
        }

        return user;
    }

    /// <summary>Maps an Identity error code onto the matching DTO field so the frontend can show
    /// the error next to that field.</summary>
    private static string MapMember(string identityErrorCode)
        => identityErrorCode.Contains("Password", StringComparison.OrdinalIgnoreCase)
           && !identityErrorCode.Contains("Incorrect", StringComparison.OrdinalIgnoreCase)
            ? nameof(ChangePasswordDto.NewPassword)
            : nameof(ChangePasswordDto.CurrentPassword);
}
