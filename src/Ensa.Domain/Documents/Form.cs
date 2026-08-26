using Ensa.Domain.Common;

namespace Ensa.Domain.Documents;

/// <summary>
/// A downloadable sample form or template, e.g. "Pre-employment Medical Examination Form".
/// <para>Legacy equivalent: <c>Form_T</c>.</para>
/// <para>
/// The <c>byte[] Document</c> + <c>DocumentName</c> + <c>DocumentType</c> triple was normalized
/// into the central <c>Document</c> table (<see cref="DocumentId"/>).
/// </para>
/// </summary>
public class Form : AuditedTenantEntity, IActivatable
{
    /// <summary>Form name.</summary>
    public string FormName { get; set; } = string.Empty;

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int? DocumentId { get; set; }

    /// <summary>Form kategorisi. FK — no navigation property. (Legacy: <c>KategoriId</c>)</summary>
    public int CategoryId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether this is the default, featured form for its category.</summary>
    public bool DefaultForm { get; set; }
}
