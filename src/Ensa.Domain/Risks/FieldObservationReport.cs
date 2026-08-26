using Ensa.Domain.Common;

namespace Ensa.Domain.Risks;

/// <summary>
/// The header of a field observation — workplace inspection round — report. (Legacy: <c>SahaGozlemRaporu_T</c>)
/// <para>
/// The legacy <c>[NotMapped] MailGonder</c> and <c>[NotMapped] MailAddress</c> fields were REMOVED
/// from the entity: they are not database columns but input parameters of the "e-mail the report
/// after saving" request, and they now live on <c>CreateFieldObservationReportDto</c>.
/// </para>
/// </summary>
public class FieldObservationReport : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company the observation was made at. FK → <c>Company.Id</c>.</summary>
    public int CompanyId { get; set; }

    /// <summary>The workplace department the observation was made in. FK → <c>WorkplaceDepartment.Id</c>. (Legacy: <c>BolumId</c>)</summary>
    public int? DepartmentId { get; set; }

    /// <summary>The date the field observation was made.</summary>
    public DateTime Date { get; set; }
}
