using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Health;

namespace Ensa.Domain.Ibys.Navigations;

/// <summary>
/// Combined read model for an IBYS query: the query plus the related workplace/employee and the
/// medical examination forms attached to it.
/// <para>
/// <c>[NotMapped]</c> — never exposed as a <c>DbSet</c> and never registered with
/// <c>ModelBuilder</c>; it is populated by projection inside
/// <c>IIbysQueryRepository.GetWithNavigationAsync</c>.
/// </para>
/// </summary>
[NotMapped]
public class IbysQueryNavigation : NavigationEntity<IbysQuery>
{
    /// <summary>Shortcut to the root record (the same instance as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public IbysQuery Query
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>Workplace the submission relates to.</summary>
    public Company? Company { get; set; }

    /// <summary>Employee the submission relates to (for health report submissions).</summary>
    public CompanyEmployee? Employee { get; set; }

    /// <summary>
    /// Medical examination forms sent with this query
    /// (through <c>MedicalExaminationForm.IbysQueryId</c>).
    /// </summary>
    public List<MedicalExaminationForm> ExaminationForms { get; set; } = [];

    /// <summary>Full name of the user who approved the submission with an e-signature (lookup — Membership module).</summary>
    public string? ApproverFullName { get; set; }
}
