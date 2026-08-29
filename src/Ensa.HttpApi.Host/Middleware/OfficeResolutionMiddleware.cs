using System.Globalization;
using System.Text.Json;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Shared.Localization;
using Ensa.Domain.Tenancy;
using Ensa.HttpApi.Filters;
using Ensa.HttpApi.Host.Ambient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Ensa.HttpApi.Host.Middleware;

/// <summary>
/// Establishes the office (branch) context for each request, from the <c>X-Ensa-OfficeId</c> header.
///
/// <para>
/// Resolution:
/// <list type="number">
/// <item>No header, an unauthenticated request, or an endpoint marked
/// <see cref="IgnoreOfficeContextAttribute"/> → no office context. The request runs exactly as it
/// did before offices existed: scoped by tenant and nothing else.</item>
/// <item>A positive integer → that office, <b>validated</b> by <see cref="IOfficeAccessManager"/>
/// against the caller's own assignments.</item>
/// <item><c>all</c> → every office the caller may use, when the server grants them that scope.</item>
/// <item>Anything else, including <c>0</c> and negative numbers → <b>400</b>. There is no silent
/// fallback to another office: a client that asked for something the server cannot honour has a bug,
/// and answering it with somebody else's data would hide it.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Ordering.</b> Registered after <c>UseAuthentication()</c> — it needs
/// <c>HttpContext.User</c> — and after <c>TenantResolutionMiddleware</c>, because which offices
/// exist is a question that can only be asked inside a tenant. It runs before
/// <c>UseAuthorization()</c>, like tenant resolution, so a request with an office it may not have is
/// refused before any endpoint code runs. It also needs <c>UseRouting()</c> to have matched an
/// endpoint already, which it has: routing is the first thing in the pipeline.
/// </para>
///
/// <para>
/// <b>Cost.</b> A request with no header costs one dictionary lookup. Only a request that actually
/// carries an office context queries anything, which is what keeps this off the path of the token
/// endpoint, the health probe and every anonymous call.
/// </para>
///
/// <para>
/// <b>The tenant is never touched.</b> The hierarchy is tenant → office → office-scoped data, and
/// switching office is a move inside one tenant, not between two.
/// </para>
/// </summary>
public sealed class OfficeResolutionMiddleware(
    RequestDelegate next,
    ILogger<OfficeResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, RequestOffice requestOffice)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestOffice);

        if (!ShouldResolve(context))
        {
            await next(context);
            return;
        }

        if (!TryParse(context, out var request, out var rawValue))
        {
            logger.LogWarning(
                "Rejected a malformed {Header} header: '{Value}'", EnsaHttpHeaders.OfficeId, rawValue);

            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Ensa:Office:InvalidHeader",
                $"'{EnsaHttpHeaders.OfficeId}' must be a positive office id or '{EnsaHttpHeaders.AllOfficesValue}'.");
            return;
        }

        if (request.Kind == OfficeContextRequestKind.None)
        {
            await next(context);
            return;
        }

        var accessManager = context.RequestServices.GetRequiredService<IOfficeAccessManager>();

        ResolvedOfficeContext resolved;
        try
        {
            resolved = await accessManager.ResolveAsync(request, context.RequestAborted);
        }
        catch (EnsaAuthorizationException exception)
        {
            // Every rejection reads the same from outside — the office does not exist, is inactive,
            // is soft-deleted, belongs to another tenant, or is simply not this user's. Separating
            // them would let a caller map out office ids in tenants they cannot see.
            logger.LogWarning(
                "Rejected an office context. UserId={UserId}, Header='{Value}', Code={Code}",
                context.User.FindFirst("sub")?.Value, rawValue, exception.Code);

            await WriteErrorAsync(
                context,
                StatusCodes.Status403Forbidden,
                exception.Code ?? "Ensa:Office:NotPermitted",
                exception.Message);
            return;
        }

        requestOffice.Set(resolved);

        logger.LogDebug(
            "Office context resolved. OfficeId={OfficeId}, AllOffices={AllOffices}",
            resolved.OfficeId, resolved.IsAllOffices);

        await next(context);
    }

    /// <summary>
    /// Whether this request can carry an office context at all. Anonymous requests cannot — there is
    /// no user to validate the office against — and an endpoint that answers "which offices may I
    /// use" must not require one.
    /// </summary>
    private static bool ShouldResolve(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return context.GetEndpoint()?.Metadata.GetMetadata<IgnoreOfficeContextAttribute>() is null;
    }

    /// <summary>
    /// Parses the header. Returns <c>false</c> only for a value that is present and malformed; an
    /// absent header is a successful parse producing <see cref="OfficeContextRequest.None"/>.
    /// </summary>
    private static bool TryParse(HttpContext context, out OfficeContextRequest request, out string? rawValue)
    {
        request = OfficeContextRequest.None;
        rawValue = null;

        if (!context.Request.Headers.TryGetValue(EnsaHttpHeaders.OfficeId, out var values))
        {
            return true;
        }

        rawValue = values.ToString();

        // An empty header is not "all offices" — that would make a client bug (a variable that
        // stringified to nothing) indistinguishable from a deliberate choice.
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var trimmed = rawValue.Trim();

        if (string.Equals(trimmed, EnsaHttpHeaders.AllOfficesValue, StringComparison.OrdinalIgnoreCase))
        {
            request = OfficeContextRequest.AllOffices;
            return true;
        }

        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var officeId)
            || officeId <= 0)
        {
            return false;
        }

        request = OfficeContextRequest.Specific(officeId);
        return true;
    }

    /// <summary>
    /// Writes the same error envelope <c>EnsaExceptionFilter</c> produces, localized the same way.
    /// The filter is an MVC filter and never sees an exception thrown out here, so the shape is
    /// reproduced rather than reused — a client must not have to parse two different error bodies
    /// from one API, and a Turkish user must not get one English sentence among Turkish ones.
    /// </summary>
    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string fallbackMessage)
    {
        var localizer = context.RequestServices.GetService<IStringLocalizer<EnsaResource>>();
        var entry = localizer?[code];
        var message = entry is { ResourceNotFound: false } ? entry.Value : fallbackMessage;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(JsonSerializer.Serialize(
            new EnsaErrorResponse { Error = new EnsaErrorInfo { Code = code, Message = message } },
            ErrorSerializerOptions));
    }

    private static readonly JsonSerializerOptions ErrorSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
