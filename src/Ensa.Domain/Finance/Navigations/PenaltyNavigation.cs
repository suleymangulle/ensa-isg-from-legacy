using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Finance.Navigations;

/// <summary>
/// Combined view of a <see cref="Penalty"/> and its amount matrix by hazard class and employee
/// count range (<see cref="PenaltyAmount"/>).
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>IPenaltyRepository.GetWithNavigationAsync</c> populates it through an <c>IQueryable</c> join
/// and projection.
/// </para>
/// </summary>
[NotMapped]
public class PenaltyNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public Penalty Penalty { get; set; } = null!;

    /// <summary>Penalty amount matrix by hazard class × employee count range × year.</summary>
    public List<PenaltyAmount> Amounts { get; set; } = [];
}
