using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Lookups.Dtos;

namespace Ensa.Application.Contracts.Lookups;

/// <summary>
/// Shared reference data behind the drop-downs of every screen: administrative geography,
/// NACE occupation codes, duties, certificates and recurrence periods.
/// <para>
/// These are host reference tables, identical for every organization and containing no
/// customer data, so they are exposed as read-only endpoints on a single service instead of
/// one service per table.
/// </para>
/// </summary>
public interface ILookupAppService : IApplicationService
{
    /// <summary>All provinces, ordered by name. Code carries the licence-plate number.</summary>
    Task<ListResultDto<LookupDto>> GetCitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Districts of one province.</summary>
    Task<ListResultDto<LookupDto>> GetDistrictsAsync(
        int cityId,
        CancellationToken cancellationToken = default);

    /// <summary>Neighbourhoods of one district.</summary>
    Task<ListResultDto<LookupDto>> GetNeighborhoodsAsync(
        int districtId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// NACE occupation code search over both the code and the activity description. The
    /// result carries the hazard class of each activity so the form can fill it in directly.
    /// </summary>
    Task<ListResultDto<OccupationCodeLookupDto>> GetOccupationCodesAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Active duty/title definitions.</summary>
    Task<ListResultDto<LookupDto>> GetDutiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Certificate type definitions.</summary>
    Task<ListResultDto<LookupDto>> GetCertificatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Recurrence period definitions used by periodic checks and plans.</summary>
    Task<ListResultDto<PeriodLookupDto>> GetPeriodsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Organization type definitions. Required when creating an organization, so a form needs
    /// them to offer a choice rather than asking for a raw identifier.
    /// </summary>
    Task<ListResultDto<LookupDto>> GetOrganizationTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Subscription plan definitions.</summary>
    Task<ListResultDto<LookupDto>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>Staff type definitions used when creating a user.</summary>
    Task<ListResultDto<LookupDto>> GetUserTypesAsync(CancellationToken cancellationToken = default);
    /// <summary>Payment method definitions — a required field on cash-register movements.</summary>
    Task<ListResultDto<LookupDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);

    /// <summary>Service item (billing article) definitions used on invoice lines.</summary>
    Task<ListResultDto<LookupDto>> GetServiceItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>Menu layout types — the codes <c>api/menu/my-menu</c> accepts.</summary>
    Task<ListResultDto<LookupDto>> GetMenuTypesAsync(CancellationToken cancellationToken = default);
}
