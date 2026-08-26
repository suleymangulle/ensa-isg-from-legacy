using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;

namespace Ensa.Application.Contracts.Trainings;

/// <summary>
/// Remote-learning progress of an employee across the trainings assigned to them.
/// </summary>
public interface IEmployeeTrainingProgressAppService : IApplicationService
{
    Task<EmployeeTrainingProgressDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>All progress records of one employee.</summary>
    Task<ListResultDto<EmployeeTrainingProgressDto>> GetForEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Progress with the employee, the training and the remaining seconds derived from the
    /// mandatory duration owned by <c>ITrainingPlanningManager</c>.
    /// </summary>
    Task<EmployeeTrainingProgressNavigationDto> GetNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the training for an employee, or returns the existing record when they have
    /// already started it — the call is idempotent.
    /// </summary>
    Task<EmployeeTrainingProgressDto> StartAsync(
        StartTrainingProgressDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records elapsed time and the current page. Elapsed time only ever moves forward, so a
    /// replayed or out-of-order client event cannot reduce recorded progress.
    /// </summary>
    Task<EmployeeTrainingProgressDto> SaveTopicProgressAsync(
        int id,
        SaveTopicProgressDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Records an exam attempt (pre-test or final test) with its score.</summary>
    Task<EmployeeTrainingProgressDto> SubmitExamAsync(
        int id,
        SubmitExamDto input,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Progress across employees, paged and filterable — the "who has not finished" view.
    /// <para>
    /// The per-employee read below answers a different question and stays; this one exists so a
    /// specialist does not have to know which employee to ask about before seeing anything.
    /// </para>
    /// </summary>
    Task<PagedResultDto<EmployeeTrainingProgressListDto>> GetListAsync(
        GetEmployeeTrainingProgressListInput input,
        CancellationToken cancellationToken = default);
}
