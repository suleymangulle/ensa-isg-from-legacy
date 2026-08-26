using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Finance;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Finance;

/// <summary>
/// Fine-risk survey application service.
/// <para>
/// The exposure figure a survey produces is a sales argument, so it must be defensible. Every
/// amount is therefore resolved on the server from the statutory catalogue using the survey's
/// own hazard class and head count; the client only ever says which article it is answering and
/// whether the workplace is in breach.
/// </para>
/// </summary>
public class PenaltySurveyAppService(
    IServiceProvider serviceProvider,
    IRepository<PenaltySurvey> penaltySurveyRepository,
    IRepository<PenaltySurveyLine> penaltySurveyLineRepository,
    IPenaltyRepository penaltyRepository)
    : EnsaAppService(serviceProvider), IPenaltySurveyAppService
{
    /// <inheritdoc />
    public async Task<PenaltySurveyDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        var survey = await penaltySurveyRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(PenaltySurvey), id);

        return ObjectMapper.Map<PenaltySurvey, PenaltySurveyDto>(survey);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<PenaltySurveyListDto>> GetListAsync(
        GetPenaltySurveyListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        var predicate = BuildSurveyFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await penaltySurveyRepository.GetCountAsync(predicate, cancellationToken);

        var records = await penaltySurveyRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<PenaltySurvey>, List<PenaltySurveyListDto>>(records);

        return new PagedResultDto<PenaltySurveyListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<PenaltySurveyDto> CreateAsync(
        CreatePenaltySurveyDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Create);

        var survey = ObjectMapper.Map<CreatePenaltySurveyDto, PenaltySurvey>(input);

        survey = await penaltySurveyRepository.InsertAsync(survey, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Fine-risk survey created: {SurveyId} — {CompanyTitle}",
            survey.Id,
            survey.CompanyTitle);

        return ObjectMapper.Map<PenaltySurvey, PenaltySurveyDto>(survey);
    }

    /// <inheritdoc />
    public async Task<PenaltySurveyDto> UpdateAsync(
        int id,
        UpdatePenaltySurveyDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Update);

        var survey = await penaltySurveyRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(PenaltySurvey), id);

        ObjectMapper.Map(input, survey);

        survey = await penaltySurveyRepository.UpdateAsync(survey, autoSave: true, cancellationToken);

        return ObjectMapper.Map<PenaltySurvey, PenaltySurveyDto>(survey);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Delete);

        var survey = await penaltySurveyRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(PenaltySurvey), id);

        var lines = await penaltySurveyLineRepository.GetListAsync(
            s => s.PenaltySurveyId == id,
            cancellationToken);

        if (lines.Count > 0)
        {
            await penaltySurveyLineRepository.DeleteManyAsync(lines, autoSave: false, cancellationToken);
        }

        await penaltySurveyRepository.DeleteAsync(survey, autoSave: true, cancellationToken);

        Logger.LogInformation("Fine-risk survey deleted: {SurveyId}", id);
    }

    // ---------------------------------------------------------------- Lines

    /// <inheritdoc />
    public async Task<PagedResultDto<PenaltySurveyLineDto>> GetLinesAsync(
        GetPenaltySurveyLineListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        _ = await penaltySurveyRepository.FindAsync(input.PenaltySurveyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(PenaltySurvey), input.PenaltySurveyId);

        var surveyId = input.PenaltySurveyId;
        var answer = input.SurveyAnswer;

        Expression<Func<PenaltySurveyLine, bool>> predicate =
            s => s.PenaltySurveyId == surveyId && (answer == null || s.SurveyAnswer == answer);

        var sorting = NormalizeSorting(input.Sorting, "Id ASC");

        var total = await penaltySurveyLineRepository.GetCountAsync(predicate, cancellationToken);

        var records = await penaltySurveyLineRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<PenaltySurveyLine>, List<PenaltySurveyLineDto>>(records);

        return new PagedResultDto<PenaltySurveyLineDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<PenaltySurveyLineDto> AddLineAsync(
        int surveyId,
        CreatePenaltySurveyLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Create);

        var survey = await penaltySurveyRepository.FindAsync(surveyId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(PenaltySurvey), surveyId);

        var penaltyId = input.PenaltyId;

        var alreadyAnswered = await penaltySurveyLineRepository.AnyAsync(
            s => s.PenaltySurveyId == surveyId && s.PenaltyId == penaltyId,
            cancellationToken);

        if (alreadyAnswered)
        {
            throw new BusinessException(
                    "This fine article has already been answered in the survey.",
                    "Ensa:Penalty:SurveyLineAlreadyExists")
                .WithData("PenaltyId", penaltyId);
        }

        var line = await BuildLineAsync(survey, input, cancellationToken);

        line = await penaltySurveyLineRepository.InsertAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<PenaltySurveyLine, PenaltySurveyLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<PenaltySurveyLineDto> UpdateLineAsync(
        int surveyId,
        int lineId,
        UpdatePenaltySurveyLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Update);

        var survey = await penaltySurveyRepository.FindAsync(surveyId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(PenaltySurvey), surveyId);

        var line = await penaltySurveyLineRepository.FindAsync(lineId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(PenaltySurveyLine), lineId);

        if (line.PenaltySurveyId != surveyId)
        {
            throw new EntityNotFoundException(typeof(PenaltySurveyLine), lineId);
        }

        var refreshed = await BuildLineAsync(survey, input, cancellationToken);

        line.PenaltyId = refreshed.PenaltyId;
        line.SurveyAnswer = refreshed.SurveyAnswer;
        line.PenaltyAmount = refreshed.PenaltyAmount;
        line.Multiplier = refreshed.Multiplier;
        line.MultiplierCalculate = refreshed.MultiplierCalculate;

        line = await penaltySurveyLineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<PenaltySurveyLine, PenaltySurveyLineDto>(line);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(
        int surveyId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Delete);

        var line = await penaltySurveyLineRepository.FindAsync(lineId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(PenaltySurveyLine), lineId);

        if (line.PenaltySurveyId != surveyId)
        {
            throw new EntityNotFoundException(typeof(PenaltySurveyLine), lineId);
        }

        await penaltySurveyLineRepository.DeleteAsync(line, autoSave: true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PenaltySurveyTotalDto> CalculateTotalAsync(
        int surveyId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        _ = await penaltySurveyRepository.FindAsync(surveyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(PenaltySurvey), surveyId);

        var lines = await penaltySurveyLineRepository.GetListAsync(
            s => s.PenaltySurveyId == surveyId,
            cancellationToken);

        var violations = lines.Where(s => s.SurveyAnswer).ToList();

        // Only breached articles carry a cost. Articles calculated per employee are multiplied by
        // the head count captured on the line at answer time, so an exposure figure quoted to a
        // prospect stays reproducible even if the survey head count is edited later.
        var total = violations.Sum(s => s.MultiplierCalculate
            ? Math.Round(s.PenaltyAmount * s.Multiplier, 2, MidpointRounding.AwayFromZero)
            : s.PenaltyAmount);

        return new PenaltySurveyTotalDto
        {
            PenaltySurveyId = surveyId,
            LineCount = lines.Count,
            ViolationCount = violations.Count,
            TotalAmount = Math.Round(total, 2, MidpointRounding.AwayFromZero)
        };
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Builds an answer line with the amount and multiplier resolved from the catalogue rather
    /// than from the request payload.
    /// </summary>
    private async Task<PenaltySurveyLine> BuildLineAsync(
        PenaltySurvey survey,
        CreatePenaltySurveyLineDto input,
        CancellationToken cancellationToken)
    {
        var penalty = await penaltyRepository.FindAsync(input.PenaltyId, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Penalty), input.PenaltyId);

        var year = input.Year ?? Clock.Now.Year;
        var workerCount = survey.WorkerCount ?? 0;
        var range = ResolveEmployeeCountRange(workerCount);

        var amount = await penaltyRepository.GetAmountAsync(
                         penalty.Id,
                         survey.HazardClass,
                         range,
                         year,
                         cancellationToken)
                     ?? throw new BusinessException(
                             "No fine amount is defined for this hazard class, head-count band and year.",
                             "Ensa:Penalty:AmountNotDefined")
                         .WithData("HazardClass", survey.HazardClass)
                         .WithData("EmployeeCountRange", range)
                         .WithData("Year", year);

        return new PenaltySurveyLine
        {
            PenaltySurveyId = survey.Id,
            PenaltyId = penalty.Id,
            SurveyAnswer = input.SurveyAnswer,
            PenaltyAmount = amount,
            MultiplierCalculate = penalty.MultiplierCalculate,
            Multiplier = penalty.MultiplierCalculate ? Math.Max(workerCount, 1) : 1m
        };
    }

    /// <summary>Maps a head count onto the band the statutory fine matrix is keyed by.</summary>
    private static EmployeeCountRange ResolveEmployeeCountRange(int workerCount) => workerCount switch
    {
        < 10 => EmployeeCountRange.FewerThanTen,
        < 50 => EmployeeCountRange.TenToFortyNine,
        _ => EmployeeCountRange.FiftyOrMore
    };

    private static Expression<Func<PenaltySurvey, bool>>? BuildSurveyFilter(GetPenaltySurveyListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var hazardClass = input.HazardClass;
        var cityId = input.CityId;

        if (search is null && hazardClass is null && cityId is null)
        {
            return null;
        }

        return a =>
            (search == null
             || a.CompanyTitle.Contains(search)
             || (a.FacilityName != null && a.FacilityName.Contains(search))
             || (a.TaxNumber != null && a.TaxNumber.Contains(search)))
            && (hazardClass == null || a.HazardClass == hazardClass)
            && (cityId == null || a.CityId == cityId);
    }
}
