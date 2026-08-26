using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance.Dtos;

namespace Ensa.Application.Contracts.Finance;

/// <summary>
/// Fine-risk surveys filled in for prospective customers: the sales team walks the fine
/// catalogue with the prospect and records, article by article, whether the workplace is in
/// breach. The resulting exposure figure is the pitch.
/// <para>
/// Unlike the fine catalogue itself, surveys belong to the organization that ran them and are
/// therefore tenant-scoped.
/// </para>
/// </summary>
public interface IPenaltySurveyAppService : IApplicationService
{
    Task<PenaltySurveyDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<PenaltySurveyListDto>> GetListAsync(
        GetPenaltySurveyListInput input,
        CancellationToken cancellationToken = default);

    Task<PenaltySurveyDto> CreateAsync(
        CreatePenaltySurveyDto input,
        CancellationToken cancellationToken = default);

    Task<PenaltySurveyDto> UpdateAsync(
        int id,
        UpdatePenaltySurveyDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the survey together with its answer lines.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------ Lines

    Task<PagedResultDto<PenaltySurveyLineDto>> GetLinesAsync(
        GetPenaltySurveyLineListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an answer. The fine amount and the head-count multiplier are resolved on the
    /// server from the catalogue using the survey's hazard class and head count, so the exposure
    /// figure cannot be manipulated from the client. An article may be answered only once per
    /// survey.
    /// </summary>
    Task<PenaltySurveyLineDto> AddLineAsync(
        int surveyId,
        CreatePenaltySurveyLineDto input,
        CancellationToken cancellationToken = default);

    Task<PenaltySurveyLineDto> UpdateLineAsync(
        int surveyId,
        int lineId,
        UpdatePenaltySurveyLineDto input,
        CancellationToken cancellationToken = default);

    Task RemoveLineAsync(int surveyId, int lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total fine exposure of the survey: the breached articles summed, with the head-count
    /// multiplier applied to the articles that are calculated per employee.
    /// </summary>
    Task<PenaltySurveyTotalDto> CalculateTotalAsync(int surveyId, CancellationToken cancellationToken = default);
}
