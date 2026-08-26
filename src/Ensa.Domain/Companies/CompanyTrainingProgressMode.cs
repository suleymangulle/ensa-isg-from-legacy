using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// The company's progression mode setting on the distance-learning portal.
/// It determines whether employees advance topic by topic or page by page.
/// <para>Legacy equivalent: <c>CompanyTrainingTransition_T</c> (PK <c>TransitionId</c>).</para>
/// </summary>
public class CompanyTrainingProgressMode : CreationAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company the setting belongs to. There is one record per company.</summary>
    public int CompanyId { get; set; }

    /// <summary>Progression mode. (Legacy: <c>ManuelGecis</c>, a string "konu"/"sayfa")</summary>
    public TrainingProgressMode TransitionMode { get; set; } = TrainingProgressMode.Topic;

    /// <summary>The user who changed the setting. (Legacy: <c>KullaniciId</c>)</summary>
    public int UserId { get; set; }

    /// <summary>The date the setting took effect. (Legacy: <c>Tarih</c>)</summary>
    public DateTime Date { get; set; }
}
