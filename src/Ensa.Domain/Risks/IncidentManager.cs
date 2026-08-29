using Ensa.Domain.Common;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Risks;

/// <summary>
/// Business rules for incident records: date validation and the SSI reporting deadline check.
/// <para>
/// Under article 13 of Law No. 5510 (Social Insurance and Universal Health Insurance) a work
/// accident must be reported to the SSI within <b>three working days</b>. For an occupational
/// disease the same three working days run from the day the disease became known.
/// Legacy performed no such check at all.
/// </para>
/// </summary>
public interface IIncidentManager : IDomainService
{
    /// <summary>
    /// Verifies that the incident can be saved. Throws <see cref="EnsaValidationException"/> when a
    /// rule is broken.
    /// </summary>
    void ValidateRecord(Incident incident);

    /// <summary>
    /// Computes the deadline for reporting the incident to the SSI: three working days from the
    /// incident date, excluding Saturdays and Sundays.
    /// Returns <c>null</c> for incident types that carry no reporting obligation.
    /// </summary>
    DateTime? CalculateLatestNotificationDate(Incident incident);

    /// <summary>
    /// Whether the SSI reporting deadline has passed. <c>true</c> when the incident has not been
    /// reported and the deadline is behind us.
    /// </summary>
    bool IsNotificationPeriodOverdue(Incident incident, DateTime? reference = null);

    /// <summary>Working days left before the reporting deadline; negative once it has passed, <c>null</c> when there is no obligation.</summary>
    int? RemainingNotificationWorkDays(Incident incident, DateTime? reference = null);
}

/// <inheritdoc cref="IIncidentManager"/>
public class IncidentManager : DomainService, IIncidentManager
{
    /// <summary>SSI work accident reporting deadline, in working days. Law No. 5510, article 13.</summary>
    public const int SsiNotificationWorkDays = 3;

    private readonly IClock _clock;

    public IncidentManager(IClock clock) => _clock = clock;

    /// <inheritdoc/>
    public void ValidateRecord(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (incident.IncidentDate > _clock.Now)
        {
            throw new EnsaValidationException(
                nameof(Incident.IncidentDate),
                "The incident date cannot be in the future.");
        }

        if (incident.ReturnToWorkDate is { } isPer && isPer.Date < incident.IncidentDate.Date)
        {
            throw new EnsaValidationException(
                nameof(Incident.ReturnToWorkDate),
                "The return-to-work date cannot precede the incident date.");
        }

        if (incident.SsiNotificationDate is { } notification && notification.Date < incident.IncidentDate.Date)
        {
            throw new EnsaValidationException(
                nameof(Incident.SsiNotificationDate),
                "The SSI notification date cannot precede the incident date.");
        }

        if (incident.LostWorkDays is < 0)
        {
            throw new EnsaValidationException(
                nameof(Incident.LostWorkDays),
                "Lost working days cannot be negative.");
        }

        // The accident type is meaningful only for incidents that are accidents.
        if (incident.IncidentType == IncidentType.OccupationalDisease && incident.AccidentType != AccidentType.Unspecified)
        {
            throw new EnsaValidationException(
                nameof(Incident.AccidentType),
                "An accident type cannot be given on an occupational disease record.");
        }
    }

    /// <inheritdoc/>
    public DateTime? CalculateLatestNotificationDate(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!IsSubjectToNotification(incident.IncidentType))
        {
            return null;
        }

        return AddWorkDays(incident.IncidentDate.Date, SsiNotificationWorkDays);
    }

    /// <inheritdoc/>
    public bool IsNotificationPeriodOverdue(Incident incident, DateTime? reference = null)
    {
        var latestDate = CalculateLatestNotificationDate(incident);
        if (latestDate is null)
        {
            return false;
        }

        // Reported on time means there is no delay.
        if (incident.SsiNotificationDate is { } notification)
        {
            return notification.Date > latestDate.Value.Date;
        }

        return (reference ?? _clock.Now).Date > latestDate.Value.Date;
    }

    /// <inheritdoc/>
    public int? RemainingNotificationWorkDays(Incident incident, DateTime? reference = null)
    {
        var latestDate = CalculateLatestNotificationDate(incident);
        if (latestDate is null)
        {
            return null;
        }

        var today = (reference ?? _clock.Now).Date;
        return (int)(latestDate.Value.Date - today).TotalDays;
    }

    /// <summary>Work accidents and occupational diseases must be reported to the SSI; near misses need not be.</summary>
    private static bool IsSubjectToNotification(IncidentType incidentType)
        => incidentType is IncidentType.WorkAccident or IncidentType.OccupationalDisease;

    /// <summary>
    /// Adds <paramref name="workDays"/> working days to the given date, skipping Saturdays and Sundays.
    /// NOTE: public holidays are not taken into account; extend this method if a holiday calendar is
    /// introduced.
    /// </summary>
    private static DateTime AddWorkDays(DateTime start, int workDays)
    {
        var date = start;
        var added = 0;

        while (added < workDays)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                added++;
            }
        }

        return date;
    }
}
