namespace Ensa.Domain.Shared.Exceptions;

/// <summary>
/// A business-rule violation that is safe to surface to the end user (ABP: <c>BusinessException</c>).
/// <para>
/// <see cref="Message"/> is the developer-facing fallback. The user-facing text is resolved at the
/// HTTP boundary from the localization resources using <see cref="Code"/> as the resource key, with
/// <see cref="Placeholders"/> substituted into the template. When no resource exists for the code,
/// <see cref="Message"/> is returned unchanged.
/// </para>
/// </summary>
public class BusinessException : Exception
{
    private Dictionary<string, object?>? _placeholders;

    public BusinessException(
        string message,
        string? code = null,
        string? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Details = details;
    }

    /// <summary>Stable error code; doubles as the localization resource key (e.g. <c>Ensa:Company:HeadquarterNotFound</c>).</summary>
    public string? Code { get; }

    /// <summary>Optional technical detail. Never localized.</summary>
    public string? Details { get; }

    /// <summary>Named values substituted into the localized template, e.g. <c>{CompanyName}</c>.</summary>
    public IReadOnlyDictionary<string, object?> Placeholders
        => _placeholders ?? (IReadOnlyDictionary<string, object?>)EmptyPlaceholders;

    private static readonly Dictionary<string, object?> EmptyPlaceholders = [];

    /// <summary>Adds a named value for the localized message template. Fluent.</summary>
    public BusinessException WithData(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        (_placeholders ??= [])[name] = value;
        return this;
    }
}

/// <summary>The requested record does not exist (ABP: <c>EntityNotFoundException</c>).</summary>
public class EntityNotFoundException : BusinessException
{
    public EntityNotFoundException(Type entityType, object? id)
        : base($"No '{entityType.Name}' record was found with key '{id}'.", "Ensa:EntityNotFound")
    {
        EntityType = entityType;
        EntityId = id;
        WithData("EntityType", entityType.Name);
        WithData("Id", id);
    }

    public Type? EntityType { get; }

    public object? EntityId { get; }
}

/// <summary>The caller is authenticated but lacks the required permission (ABP: <c>AbpAuthorizationException</c>).</summary>
public class EnsaAuthorizationException(string? message = null, string? code = null)
    : BusinessException(
        message ?? "You are not authorized to perform this operation.",
        code ?? "Ensa:Authorization");

/// <summary>One or more input fields failed validation.</summary>
public class EnsaValidationException : BusinessException
{
    public EnsaValidationException(IReadOnlyList<ValidationError> errors)
        : base("The submitted values are not valid.", "Ensa:Validation")
        => Errors = errors;

    public EnsaValidationException(string member, string message)
        : this([new ValidationError(member, message)])
    {
    }

    public IReadOnlyList<ValidationError> Errors { get; }
}

/// <summary>A single field-level validation failure.</summary>
/// <param name="Member">Property name the error belongs to.</param>
/// <param name="Message">Human-readable, already-localized description.</param>
public sealed record ValidationError(string Member, string Message);

/// <summary>An operation that requires a tenant context was invoked in the host context.</summary>
public class TenantRequiredException() : BusinessException(
    "This operation requires an active organization (tenant) context.",
    "Ensa:TenantRequired");
