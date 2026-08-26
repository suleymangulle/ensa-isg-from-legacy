using System.ComponentModel.DataAnnotations.Schema;

namespace Ensa.Domain.Companies.Navigations;

/// <summary>
/// Projection of an employee's most recent attendance record for a training subject.
/// <para>
/// The legacy <c>CompanyEmployeeLatestTrainings</c> and <c>EmployeeLatestTrainingDates</c>
/// classes were NOT tables but the result types of a <c>GROUP BY ... MAX(DocumentDate)</c> query.
/// They were therefore not turned into entities but merged into this single
/// <see cref="NotMappedAttribute"/> projection class. It never becomes a <c>DbSet</c>; the
/// repository populates it by projection.
/// </para>
/// </summary>
[NotMapped]
public class EmployeeLatestTrainingInfo
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>Employee first name (carried along to avoid a join on list screens).</summary>
    public string? Name { get; set; }

    public string? LastName { get; set; }

    /// <summary>The training definition. May be empty on the source record.</summary>
    public int? TrainingId { get; set; }

    /// <summary>Date of the most recent attendance certificate.</summary>
    public DateTime? TrainingDate { get; set; }

    /// <summary>Id of the <see cref="CompanyEmployeeDocument"/> record the date came from.</summary>
    public int? CompanyEmployeeDocumentId { get; set; }
}
