using Ensa.Domain.Common;

namespace Ensa.Domain.Health;

/// <summary>
/// SKRS medication reference (the e-prescription drug list).
/// <para>Legacy equivalent: <c>SKRS_Medication_T</c> (PK <c>MedicationId</c> → <c>Id</c>).</para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>; seeded by <c>DbMigrator</c>.</para>
/// </summary>
public class Medication : AuditedEntity, IActivatable
{
    /// <summary>Trade name of the medication. (Legacy: <c>IlacAdi</c>)</summary>
    public string MedicationName { get; set; } = string.Empty;

    /// <summary>Medication barcode — prescription lines are matched on this value. (Legacy: <c>Barkodu</c>)</summary>
    public string? Barcode { get; set; }

    /// <summary>Name of the marketing authorisation holder. (Legacy: <c>FirmaAdi</c>)</summary>
    public string? GeneratorCompanyName { get; set; }

    /// <summary>ATC classification code. (Legacy: <c>ATC_Kodu</c>)</summary>
    public string? AtcCode { get; set; }

    /// <summary>ATC classification name. (Legacy: <c>ATC_Adi</c>)</summary>
    public string? AtcName { get; set; }

    /// <summary>Reimbursement condition for outpatient treatment. (Legacy: <c>AyaktanOdenmeSarti</c>)</summary>
    public string? OutpatientReimbursementCondition { get; set; }

    /// <summary>Reimbursement condition for inpatient treatment. (Legacy: <c>YatanOdenmeSarti</c>)</summary>
    public string? InpatientReimbursementCondition { get; set; }

    /// <summary>Prescription class (normal, green, red, ...). (Legacy: <c>ReceteTuru</c>)</summary>
    public string? PrescriptionType { get; set; }

    /// <summary>Date the medication was deactivated. (Legacy: <c>PasifeAlmaTarihi</c>)</summary>
    public DateTime? DeactivationDate { get; set; }

    /// <summary>Whether the medication can still be prescribed. (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
