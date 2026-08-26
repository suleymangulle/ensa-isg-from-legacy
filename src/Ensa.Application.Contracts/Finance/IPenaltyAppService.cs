using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Finance;

/// <summary>
/// Catalogue of the administrative fines laid down by Law 6331 and its regulations.
/// <para>
/// These records live in the HOST context: they are the same for every organization, so reads
/// are open to any authenticated user with <c>Ensa.Penalty</c> while writes are administrative.
/// Each article carries a normalized amount matrix keyed by hazard class, head-count band and
/// year, which is what makes historic fine amounts reproducible after the annual revaluation.
/// </para>
/// </summary>
public interface IPenaltyAppService : IApplicationService
{
    Task<PenaltyDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The article together with its full amount matrix.</summary>
    Task<PenaltyNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<PenaltyListDto>> GetListAsync(
        GetPenaltyListInput input,
        CancellationToken cancellationToken = default);

    Task<PenaltyDto> CreateAsync(CreatePenaltyDto input, CancellationToken cancellationToken = default);

    Task<PenaltyDto> UpdateAsync(int id, UpdatePenaltyDto input, CancellationToken cancellationToken = default);

    /// <summary>Deletes the article together with its amount matrix.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ---------------------------------------------------------- Amount matrix

    /// <summary>The amount matrix of one article, newest year first.</summary>
    Task<ListResultDto<PenaltyAmountDto>> GetAmountsAsync(
        int penaltyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a matrix cell. The combination of article, hazard class, head-count band and year is
    /// unique, so a duplicate is refused rather than silently shadowing the existing amount.
    /// </summary>
    Task<PenaltyAmountDto> AddAmountAsync(
        int penaltyId,
        CreatePenaltyAmountDto input,
        CancellationToken cancellationToken = default);

    Task<PenaltyAmountDto> UpdateAmountAsync(
        int penaltyId,
        int amountId,
        UpdatePenaltyAmountDto input,
        CancellationToken cancellationToken = default);

    Task RemoveAmountAsync(int penaltyId, int amountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The amount that applies to a workplace profile for a given year. When the requested year
    /// has no row the closest earlier year is used, which is how a fine schedule that has not yet
    /// been revalued for the current year still yields a usable figure.
    /// </summary>
    Task<ApplicablePenaltyAmountDto> GetApplicableAmountAsync(
        int penaltyId,
        HazardClass hazardClass,
        EmployeeCountRange range,
        int year,
        CancellationToken cancellationToken = default);
}
