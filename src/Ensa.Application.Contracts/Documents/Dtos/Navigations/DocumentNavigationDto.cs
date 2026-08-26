using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Documents.Dtos.Navigations;

/// <summary>
/// The combined view the document detail screen needs in a single call.
/// <para>
/// Mirrors <c>Ensa.Domain.Documents.Navigations.DocumentNavigation</c>. Class-typed properties
/// are not allowed on plain DTOs, so this combination lives in a <see cref="NavigationDto"/>
/// derivative instead (see docs/ARCHITECTURE.md §4).
/// </para>
/// </summary>
public class DocumentNavigationDto : NavigationDto
{
    public DocumentDto Document { get; set; } = null!;

    /// <summary>The category the document is filed under.</summary>
    public LookupDto? Category { get; set; }

    /// <summary>The company the document belongs to; <c>null</c> for tenant-level documents.</summary>
    public LookupDto? Company { get; set; }
}
