using Ensa.Domain.Common;

namespace Ensa.Domain.Documents;

/// <summary>
/// Reference for a standard document type requested from companies on a recurring basis,
/// e.g. a signature circular or a tax certificate.
/// <para>Legacy equivalent: <c>StandardDocuments_T</c>.</para>
/// <para>A host reference table, with no tenant.</para>
/// </summary>
public class StandardDocument : AuditedEntity, IActivatable
{
    /// <summary>Document name.</summary>
    public string StandardDocumentName { get; set; } = string.Empty;

    /// <summary>Document code. (Legacy: <c>SabitEvraKodu</c> — the typo was fixed.)</summary>
    public string StandardDocumentCode { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
