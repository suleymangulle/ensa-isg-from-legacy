using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ensa.HttpApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Ensa.HttpApi;

/// <summary>
/// DI registration for the HttpApi layer (the counterpart of ABP's <c>EnsaHttpApiModule : AbpModule</c>).
/// </summary>
public static class EnsaHttpApiModule
{
    /// <summary>
    /// Registers the controllers, the global exception filter and the JSON options.
    /// <para>
    /// <b>JSON contract (shared with the frontend):</b>
    /// <list type="bullet">
    /// <item>Property names are <c>camelCase</c>.</item>
    /// <item>Enums are serialized <b>as numbers (int)</b> — <c>JsonStringEnumConverter</c> is NOT used.</item>
    /// <item>Reference cycles are ignored (<c>ReferenceHandler.IgnoreCycles</c>).</item>
    /// <item>Validation failures are returned in the same <c>{ "error": { ... } }</c> envelope.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static IServiceCollection AddEnsaHttpApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        // Resource files live in Ensa.Domain.Shared/Localization (EnsaResource.resx / .tr.resx).
        services.AddLocalization();

        services
            .AddControllers(options =>
            {
                options.Filters.Add<EnsaExceptionFilter>();
                options.ReturnHttpNotAcceptable = true;

                // Controller names become kebab-case route segments:
                // CompanyEmployeeController -> api/company-employee.
                // Without this, [controller] would render "companyemployee", which is both ugly
                // and inconsistent with the URLs the SPA is written against.
                options.Conventions.Add(
                    new RouteTokenTransformerConvention(new KebabCaseParameterTransformer()));
            })
            .AddApplicationPart(typeof(EnsaHttpApiModule).Assembly)
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

        // Turn DataAnnotations validation failures into that same error envelope.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var validationErrors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .SelectMany(entry => entry.Value!.Errors.Select(error => new EnsaValidationErrorInfo
                    {
                        Member = entry.Key,
                        Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The value is not valid."
                            : error.ErrorMessage
                    }))
                    .ToList();

                var response = new EnsaErrorResponse
                {
                    Error = new EnsaErrorInfo
                    {
                        Code = "Ensa:Validation",
                        Message = "The submitted values are not valid.",
                        ValidationErrors = validationErrors
                    }
                };

                return new BadRequestObjectResult(response)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            };
        });

        return services;
    }

    /// <summary>The Ensa JSON contract. Usable by the minimal APIs in the host as well.</summary>
    public static void ConfigureJson(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

        // IMPORTANT: enums go over the wire as numbers. The TypeScript frontend expects ints
        // in the `enum X { A = 1 }` form; do NOT add JsonStringEnumConverter.
    }
}

/// <summary>
/// Turns a route token such as <c>CompanyEmployee</c> into <c>company-employee</c>.
/// <para>
/// Applied through <see cref="RouteTokenTransformerConvention"/> so that every controller gets a
/// kebab-case URL without each one repeating an explicit <c>[Route]</c> attribute.
/// </para>
/// </summary>
public sealed partial class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var text = value.ToString();
        return string.IsNullOrEmpty(text)
            ? text
            : BoundaryPattern().Replace(text, "$1-$2").ToLowerInvariant();
    }

    /// <summary>Matches a lower-case/digit followed by an upper-case letter — the word boundary.</summary>
    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex BoundaryPattern();
}
