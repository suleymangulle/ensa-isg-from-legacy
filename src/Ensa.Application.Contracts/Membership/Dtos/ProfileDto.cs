using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Membership.Dtos;

/// <summary>
/// Profile information for the signed-in user.
/// The frontend calls this endpoint after login to establish the user context.
/// </summary>
public class ProfileDto
{
    /// <summary>User id (the <c>sub</c> claim in the token).</summary>
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Full name shown on screen.</summary>
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Gsm { get; set; }

    /// <summary>User photo — FK to the <c>Document</c> table.</summary>
    public int? PhotoDocumentId { get; set; }

    /// <summary>Colour (hex) representing the user on the calendar and planning screens.</summary>
    public string? Color { get; set; }

    /// <summary>The organization the user belongs to. <c>null</c> means a host (system) user.</summary>
    public int? TenantId { get; set; }

    public int? OfficeId { get; set; }

    /// <summary>The company this user belongs to, when the user is a customer company user.</summary>
    public int? CompanyId { get; set; }

    public StaffRole StaffRole { get; set; }

    /// <summary>System administrator — holds every permission and may switch tenants.</summary>
    public bool SystemAdministrator { get; set; }

    /// <summary>Organization (tenant) administrator.</summary>
    public bool OrganizationAdmin { get; set; }

    /// <summary>Whether the user administers their own office.</summary>
    public bool OfficeAdmin { get; set; }

    /// <summary>Roles held by the user.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>The user cannot continue without changing their password (Legacy: <c>PasswordDegisti</c>).</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Whether the terms of use have been accepted.</summary>
    public bool ContractApproved { get; set; }

    /// <summary>When the temporary account lockout ends, if the account is locked.</summary>
    public DateTimeOffset? LockoutEnd { get; set; }
}
