using System.Text.Json.Serialization;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Shared.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ensa.HttpApi.Filters;

/// <summary>
/// Turns every exception into one uniform, ABP-style response body:
/// <code>
/// { "error": { "code": "...", "message": "...", "details": "...", "validationErrors": [ ... ] } }
/// </code>
/// <para>
/// Mapping:
/// <list type="bullet">
/// <item><see cref="EntityNotFoundException"/> → 404</item>
/// <item><see cref="EnsaAuthorizationException"/> → 403</item>
/// <item><see cref="EnsaValidationException"/> → 400 (with the per-field error list)</item>
/// <item><see cref="BusinessException"/> → 400</item>
/// <item><see cref="OperationCanceledException"/> → 499 (the client cancelled the request)</item>
/// <item>anything else → 500 (details are returned only in the Development environment)</item>
/// </list>
/// </para>
/// </summary>
public sealed class EnsaExceptionFilter(
    ILogger<EnsaExceptionFilter> logger,
    IHostEnvironment environment,
    IStringLocalizer<EnsaResource> localizer) : IExceptionFilter
{
    /// <summary>
    /// Resolves the user-facing text for an error code.
    /// <para>
    /// Looks the code up in the localization resources for the request culture and substitutes
    /// named placeholders (<c>{CompanyName}</c>) from <see cref="BusinessException.Placeholders"/>.
    /// When the code has no resource entry, the exception's own message is returned so that a
    /// missing translation degrades to English rather than to an empty string.
    /// </para>
    /// </summary>
    private string Localize(string? code, string fallback, IReadOnlyDictionary<string, object?>? data = null)
    {
        if (string.IsNullOrEmpty(code))
        {
            return fallback;
        }

        var entry = localizer[code];
        if (entry.ResourceNotFound)
        {
            return fallback;
        }

        var text = entry.Value;
        if (data is null || data.Count == 0)
        {
            return text;
        }

        foreach (var (name, value) in data)
        {
            text = text.Replace(
                "{" + name + "}",
                value?.ToString() ?? string.Empty,
                StringComparison.Ordinal);
        }

        return text;
    }

    /// <summary>The (non-standard) status code used when the client cancels the request.</summary>
    private const int StatusClientClosedRequest = 499;

    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (statusCode, payload) = Map(context.Exception);

        Log(context, statusCode);

        context.Result = new ObjectResult(new EnsaErrorResponse { Error = payload })
        {
            StatusCode = statusCode
        };
        context.HttpContext.Response.StatusCode = statusCode;
        context.ExceptionHandled = true;
    }

    private (int StatusCode, EnsaErrorInfo Error) Map(Exception exception) => exception switch
    {
        EntityNotFoundException ex => (StatusCodes.Status404NotFound, new EnsaErrorInfo
        {
            Code = ex.Code ?? "Ensa:EntityNotFound",
            Message = Localize(ex.Code ?? "Ensa:EntityNotFound", ex.Message, ex.Placeholders),
            Details = ex.Details
        }),

        EnsaAuthorizationException ex => (StatusCodes.Status403Forbidden, new EnsaErrorInfo
        {
            Code = ex.Code ?? "Ensa:Authorization",
            Message = Localize(ex.Code ?? "Ensa:Authorization", ex.Message, ex.Placeholders),
            Details = ex.Details
        }),

        EnsaValidationException ex => (StatusCodes.Status400BadRequest, new EnsaErrorInfo
        {
            Code = ex.Code ?? "Ensa:Validation",
            Message = Localize(ex.Code ?? "Ensa:Validation", ex.Message, ex.Placeholders),
            Details = ex.Details,
            ValidationErrors = [.. ex.Errors.Select(e => new EnsaValidationErrorInfo
            {
                Member = e.Member,
                Message = e.Message
            })]
        }),

        BusinessException ex => (StatusCodes.Status400BadRequest, new EnsaErrorInfo
        {
            Code = ex.Code ?? "Ensa:BusinessError",
            Message = Localize(ex.Code ?? "Ensa:BusinessError", ex.Message, ex.Placeholders),
            Details = ex.Details
        }),

        OperationCanceledException => (StatusClientClosedRequest, new EnsaErrorInfo
        {
            Code = "Ensa:RequestCancelled",
            Message = Localize("Ensa:RequestCancelled", "The request was cancelled.")
        }),

        _ => (StatusCodes.Status500InternalServerError, new EnsaErrorInfo
        {
            Code = "Ensa:InternalServerError",
            Message = Localize(
                "Ensa:InternalServerError",
                "An unexpected error occurred. Please contact your system administrator."),
            // The stack trace and inner message leave the process only in development.
            Details = environment.IsDevelopment() ? exception.ToString() : null
        })
    };

    private void Log(ExceptionContext context, int statusCode)
    {
        const string template =
            "Request failed. {Method} {Path} -> {StatusCode}. TraceId={TraceId}";

        var method = context.HttpContext.Request.Method;
        var path = context.HttpContext.Request.Path.Value;
        var traceId = context.HttpContext.TraceIdentifier;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(context.Exception, template, method, path, statusCode, traceId);
        }
        else if (statusCode == StatusClientClosedRequest)
        {
            logger.LogDebug(template, method, path, statusCode, traceId);
        }
        else
        {
            logger.LogWarning(template, method, path, statusCode, traceId);
        }
    }
}

/// <summary>ABP-compatible error envelope.</summary>
public sealed class EnsaErrorResponse
{
    [JsonPropertyName("error")]
    public EnsaErrorInfo Error { get; set; } = new();
}

/// <summary>The error body itself.</summary>
public sealed class EnsaErrorInfo
{
    /// <summary>Machine-readable error code (e.g. <c>Ensa:EntityNotFound</c>).</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Message that is safe to show to the end user.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Additional explanation (optional).</summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>Per-field validation errors (populated only for 400/validation responses).</summary>
    [JsonPropertyName("validationErrors")]
    public IReadOnlyList<EnsaValidationErrorInfo>? ValidationErrors { get; set; }
}

/// <summary>A validation error for one single field.</summary>
public sealed class EnsaValidationErrorInfo
{
    [JsonPropertyName("member")]
    public string Member { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
