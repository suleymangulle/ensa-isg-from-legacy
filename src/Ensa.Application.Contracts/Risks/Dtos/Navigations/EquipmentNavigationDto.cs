using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Risks.Dtos.Navigations;

/// <summary>
/// Work equipment combined with its company, inspection report and attached documents.
/// Mirrors <c>Ensa.Domain.Risks.Navigations.EquipmentNavigation</c>.
/// </summary>
public class EquipmentNavigationDto : NavigationDto
{
    /// <summary>Root equipment record.</summary>
    public EquipmentDto Equipment { get; set; } = null!;

    /// <summary>Company the equipment belongs to.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Latest periodic inspection report file.</summary>
    public LookupDto? ExaminationReportDocument { get; set; }

    /// <summary>Documents attached to the equipment.</summary>
    public List<EquipmentDocumentNavigationDto> Documents { get; set; } = [];
}

/// <summary>Equipment document together with its file and type definition.</summary>
public class EquipmentDocumentNavigationDto : NavigationDto
{
    /// <summary>Root equipment document record.</summary>
    public EquipmentDocumentDto Document { get; set; } = null!;

    /// <summary>Underlying file.</summary>
    public LookupDto? File { get; set; }

    /// <summary>Document type definition (tenant-specific lookup).</summary>
    public LookupDto? DocumentType { get; set; }
}
