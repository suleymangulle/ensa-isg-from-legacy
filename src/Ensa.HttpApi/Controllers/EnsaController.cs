using System.Globalization;
using System.Security.Claims;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Base class for every Ensa API controller.
/// <para>
/// <b>Authentication is required by default</b>; endpoints that need anonymous
/// access are marked explicitly with <c>[AllowAnonymous]</c>.
/// </para>
/// <para>
/// Route convention: <c>api/{controller}</c> — e.g. <c>CompanyController</c> → <c>api/company</c>.
/// </para>
/// <example>
/// <code>
/// public class CompanyController(ICompanyAppService companyAppService) : EnsaController
/// {
///     [HttpGet("{id:int}")]
///     [Authorize(EnsaPermissions.Company.Default)]
///     [ProducesResponseType&lt;CompanyDto&gt;(StatusCodes.Status200OK)]
///     public Task&lt;CompanyDto&gt; GetAsync(int id, CancellationToken ct)
///         =&gt; companyAppService.GetAsync(id, ct);
/// }
/// </code>
/// </example>
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public abstract class EnsaController : ControllerBase
{
    /// <summary>User id resolved from the <c>sub</c> claim on the token.</summary>
    protected int? CurrentUserId => ParseInt(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>The <c>ensa:tenantId</c> claim on the token. <c>null</c> means the host context.</summary>
    protected int? CurrentTenantId => ParseInt(User.FindFirstValue(EnsaClaimTypes.TenantId));

    /// <summary>Per-request correlation id, used for logging and tracing.</summary>
    protected string CorrelationId =>
        Request.Headers.TryGetValue(EnsaHttpHeaders.CorrelationId, out var value)
        && !string.IsNullOrWhiteSpace(value.ToString())
            ? value.ToString()
            : HttpContext.TraceIdentifier;

    private static int? ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}
