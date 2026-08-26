using System.ComponentModel.DataAnnotations;

namespace Ensa.Application.Contracts.Membership.Dtos;

/// <summary>Input for the signed-in user changing their own password.</summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "The current password is required.")]
    [StringLength(128, MinimumLength = 1)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "The new password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "The new password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "The new password confirmation is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and its confirmation must match.")]
    public string NewPasswordRepeat { get; set; } = string.Empty;
}
