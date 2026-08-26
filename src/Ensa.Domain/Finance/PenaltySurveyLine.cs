using Ensa.Domain.Common;

namespace Ensa.Domain.Finance;

/// <summary>
/// The answer given to a single <see cref="Penalty"/> article on a <see cref="PenaltySurvey"/> form.
/// <para>Legacy equivalent: <c>PenaltySurveyItem_T</c>.</para>
/// </summary>
public class PenaltySurveyLine : CreationAuditedTenantEntity
{
    /// <summary>FK — no navigation property.</summary>
    public int PenaltySurveyId { get; set; }

    /// <summary>FK — no navigation property.</summary>
    public int PenaltyId { get; set; }

    /// <summary>Whether the workplace breaches this article — the survey answer.</summary>
    public bool SurveyAnswer { get; set; }

    /// <summary>The calculated penalty amount. (Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal PenaltyAmount { get; set; }

    /// <summary>(Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal Multiplier { get; set; }

    public bool MultiplierCalculate { get; set; }
}
