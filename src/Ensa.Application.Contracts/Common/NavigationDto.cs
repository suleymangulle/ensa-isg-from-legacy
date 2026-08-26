namespace Ensa.Application.Contracts.Common;

/// <summary>
/// An ABP-style "navigation DTO".
/// <para>
/// Class-typed properties are not allowed on plain DTOs, so derivatives of this class are used
/// wherever several records have to be presented together.
/// </para>
/// <para>
/// RULES:
/// <list type="bullet">
/// <item>Naming: <c>{Entity}NavigationDto</c>, for example <c>CompanyNavigationDto</c>.</item>
/// <item>Populated by projection, in the application layer only.</item>
/// <item>Never mapped back to an entity; it is a read-side (query) type only.</item>
/// </list>
/// </para>
/// </summary>
public abstract class NavigationDto
{
}

/// <summary>A navigation DTO that carries a single root DTO.</summary>
public abstract class NavigationDto<TDto> : NavigationDto
    where TDto : class
{
    /// <summary>The root DTO.</summary>
    public TDto Entity { get; set; } = default!;
}

/// <summary>
/// A lightweight record for lookup lists and reference display.
/// Prefer this over a full DTO inside navigation DTOs.
/// </summary>
public class LookupDto<TKey>
{
    public TKey Id { get; set; } = default!;
    public string DisplayName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LookupDto : LookupDto<int>;
