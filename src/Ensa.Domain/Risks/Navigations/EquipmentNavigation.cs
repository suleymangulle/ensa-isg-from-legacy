using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Risks.Navigations;

/// <summary>
/// Combined view of a piece of equipment with its company, inspection report and documents.
/// </summary>
[NotMapped]
public class EquipmentNavigation : NavigationEntity
{
    /// <summary>The root equipment record.</summary>
    public Equipment Equipment { get; set; } = null!;

    /// <summary>Summary of the company the equipment is located at.</summary>
    public Company? Company { get; set; }

    /// <summary>Son periyodik muayene raporu belgesi (legacy <c>byte[] PerExaminationReportDocument</c>).</summary>
    public Document? ExaminationReportDocument { get; set; }

    /// <summary>Documents attached to the equipment.</summary>
    public List<EquipmentDocumentNavigation> Documents { get; set; } = [];
}

/// <summary>Combined view of an equipment document, its file and its type definition.</summary>
[NotMapped]
public class EquipmentDocumentNavigation : NavigationEntity
{
    /// <summary>The root document record.</summary>
    public EquipmentDocument Document { get; set; } = null!;

    /// <summary>The document's file.</summary>
    public Document? File { get; set; }

    /// <summary>The document's type definition (legacy <c>EquipmentDocumentList_T</c>).</summary>
    public EquipmentDocumentType? DocumentType { get; set; }
}
