using System.Globalization;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Domain.Membership;
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
    IPermissionResolver permissionResolver)
    : EnsaAppService(serviceProvider), IAccountAppService
{
    /// <inheritdoc />
    public async Task<ProfileDto> GetProfileAsync()
    {
        var user = await GetCurrentUserAsync();
        var roles = await userManager.GetRolesAsync(user);

        return new ProfileDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Name = user.Name,
            LastName = user.LastName,
            FullName = user.GetDisplayName(),
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            Gsm = user.Gsm,
            PhotoDocumentId = user.PhotoDocumentId,
            Color = user.Color,
            TenantId = user.TenantId,
            OfficeId = user.OfficeId,
            CompanyId = user.CompanyId,
            StaffRole = user.StaffRole,
            SystemAdministrator = user.SystemAdministrator,
            OrganizationAdmin = user.OrganizationAdmin,
            OfficeAdmin = user.OfficeAdmin,
            Roles = [.. roles],
            MustChangePassword = user.MustChangePassword,
            ContractApproved = user.ContractApproved,
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

        if (!user.CanSignIn())
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
