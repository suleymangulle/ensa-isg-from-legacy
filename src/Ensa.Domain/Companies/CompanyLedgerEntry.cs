using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Ledger movement for a company (a debit or credit entry).
/// <para>Legacy equivalent: <c>CompanyHareket_T</c>.</para>
/// <para>
/// NORMALISATION: the separate legacy <c>Debit</c> and <c>Credit</c> <c>double</c> columns were
/// reduced to the single <see cref="Amount"/> + <see cref="LedgerEntryType"/> pair.
/// The balance is computed as total credit minus total debit.
/// </para>
/// </summary>
public class CompanyLedgerEntry : AuditedTenantEntity, ICompanyScoped
{
    public int CompanyId { get; set; }

    /// <summary>Ledger (accounting) date of the movement.</summary>
    public DateTime Date { get; set; }

    /// <summary>Direction of the movement. (Legacy: the <c>Borc</c>/<c>Credit</c> column pair)</summary>
    public LedgerEntryType LedgerEntryType { get; set; }

    /// <summary>Amount of the movement — always positive; the direction comes from <see cref="LedgerEntryType"/>.</summary>
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    /// <summary>Whether the entry belongs to the official or the unofficial account.</summary>
    public bool OfficialAccount { get; set; }

    /// <summary>The module that produced the movement. (Legacy: <c>Modul</c> string)</summary>
    public SourceModule SourceModule { get; set; } = SourceModule.Unspecified;

    /// <summary>Id of the record in the source module (e.g. the invoice or the collection).</summary>
    public int? OperationId { get; set; }
}
