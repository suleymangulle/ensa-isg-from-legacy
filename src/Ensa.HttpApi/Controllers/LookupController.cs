using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Lookups;
using Ensa.Application.Contracts.Lookups.Dtos;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Shared reference data endpoints - <c>api/lookup</c>.
/// <para>
/// Every endpoint requires <c>Ensa.Lookups</c>, the read permission of the reference module.
/// Screen-specific permissions were rejected because the same province or NACE list feeds a
/// dozen unrelated forms, and open access was rejected because the curated NACE catalogue with
/// its hazard classes should not be freely scrapeable. See <c>LookupAppService</c> for the full
/// reasoning.
/// </para>
/// </summary>
public class LookupController(ILookupAppService lookupAppService) : EnsaController
{
    /// <summary>All provinces.</summary>
    [HttpGet("cities")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetCitiesAsync(CancellationToken cancellationToken)
        => lookupAppService.GetCitiesAsync(cancellationToken);

    /// <summary>Districts of one province.</summary>
    [HttpGet("cities/{cityId:int}/districts")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetDistrictsAsync(
        int cityId,
        CancellationToken cancellationToken)
        => lookupAppService.GetDistrictsAsync(cityId, cancellationToken);

    /// <summary>Neighbourhoods of one district.</summary>
    [HttpGet("districts/{districtId:int}/neighborhoods")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetNeighborhoodsAsync(
        int districtId,
        CancellationToken cancellationToken)
        => lookupAppService.GetNeighborhoodsAsync(districtId, cancellationToken);

    /// <summary>NACE occupation code search; the result carries the hazard class.</summary>
    [HttpGet("occupation-codes")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<OccupationCodeLookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<OccupationCodeLookupDto>> GetOccupationCodesAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => lookupAppService.GetOccupationCodesAsync(filter, cancellationToken);

    /// <summary>Active duty/title definitions.</summary>
    [HttpGet("duties")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetDutiesAsync(CancellationToken cancellationToken)
        => lookupAppService.GetDutiesAsync(cancellationToken);

    /// <summary>Certificate type definitions.</summary>
    [HttpGet("certificates")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetCertificatesAsync(CancellationToken cancellationToken)
        => lookupAppService.GetCertificatesAsync(cancellationToken);

    /// <summary>Recurrence period definitions.</summary>
    [HttpGet("periods")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<PeriodLookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<PeriodLookupDto>> GetPeriodsAsync(CancellationToken cancellationToken)
        => lookupAppService.GetPeriodsAsync(cancellationToken);

    /// <summary>Organization type definitions.</summary>
    [HttpGet("organization-types")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetOrganizationTypesAsync(CancellationToken cancellationToken)
        => lookupAppService.GetOrganizationTypesAsync(cancellationToken);

    /// <summary>Subscription plan definitions.</summary>
    [HttpGet("subscription-plans")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetSubscriptionPlansAsync(CancellationToken cancellationToken)
        => lookupAppService.GetSubscriptionPlansAsync(cancellationToken);

    /// <summary>Staff type definitions.</summary>
    [HttpGet("user-types")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetUserTypesAsync(CancellationToken cancellationToken)
        => lookupAppService.GetUserTypesAsync(cancellationToken);
    /// <summary>Payment method definitions.</summary>
    [HttpGet("payment-methods")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken)
        => lookupAppService.GetPaymentMethodsAsync(cancellationToken);

    /// <summary>Service item definitions used on invoice lines.</summary>
    [HttpGet("service-items")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetServiceItemsAsync(CancellationToken cancellationToken)
        => lookupAppService.GetServiceItemsAsync(cancellationToken);

    /// <summary>Menu layout types.</summary>
    [HttpGet("menu-types")]
    [Authorize(EnsaPermissions.Lookups.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetMenuTypesAsync(CancellationToken cancellationToken)
        => lookupAppService.GetMenuTypesAsync(cancellationToken);
}
