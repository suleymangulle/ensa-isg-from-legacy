using Ensa.Domain.Repositories;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>Queries specific to equipment records (legacy: Cihaz).</summary>
public interface IEquipmentRepository : IRepository<Equipment>
{
    /// <summary>Loads the equipment with its company, inspection report file and documents.</summary>
    Task<EquipmentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists equipment whose periodic inspection is overdue: either
    /// <c>NextExaminationDate &lt; reference</c>, or never inspected at all.
    /// </summary>
    /// <param name="reference">Reference date.</param>
    /// <param name="companyId">Optional; narrows the result to a single company.</param>
    /// <param name="includeNeverExamined">
    /// When <c>true</c>, equipment with <c>ExaminationDate == null</c> is included in the result.
    /// </param>
    Task<List<Equipment>> GetExaminationOverdueAsync(
        DateTime reference,
        int? companyId = null,
        bool includeNeverExamined = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists equipment whose inspection falls due within <paramref name="dayCount"/> days, for
    /// reminders.
    /// </summary>
    Task<List<Equipment>> GetExaminationUpcomingAsync(
        DateTime reference,
        int dayCount,
        int? companyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists a company's equipment, filtered by type.</summary>
    Task<List<Equipment>> GetListByCompanyAsync(
        int companyId,
        EquipmentType? equipmentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the documents attached to a piece of equipment.</summary>
    Task<List<EquipmentDocument>> GetDocumentsAsync(
        int equipmentId,
        CancellationToken cancellationToken = default);
}
