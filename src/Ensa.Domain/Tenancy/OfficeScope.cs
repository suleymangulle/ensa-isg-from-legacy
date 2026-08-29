using Ensa.Domain.Common;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// The office restriction one query should run under, after the request's validated office context
/// and any office filter the caller put in the request body/query string have been reconciled.
/// </summary>
/// <param name="OfficeIds">
/// The ids to restrict to. <b>Empty means no office predicate</b> — the tenant filter is the only
/// boundary, which is what every one of these queries did before the office context existed.
/// </param>
public readonly record struct OfficeQueryScope(IReadOnlyList<int> OfficeIds)
{
    /// <summary>No office restriction.</summary>
    public static readonly OfficeQueryScope Unrestricted = new([]);

    /// <summary>Whether a predicate has to be applied at all.</summary>
    public bool IsRestricted => OfficeIds.Count > 0;

    /// <summary>
    /// The single office to compare against, when the scope is exactly one office. Lets a caller
    /// emit <c>x.OfficeId == id</c> instead of a one-element <c>IN</c> list.
    /// </summary>
    public int? SingleOfficeId => OfficeIds.Count == 1 ? OfficeIds[0] : null;
}

/// <summary>
/// Turns the request's office context into a query restriction — the one place that decides what an
/// office-scoped query is allowed to see.
/// </summary>
public static class OfficeScope
{
    /// <summary>
    /// Reconciles the validated office context with an office id the caller also put in the request
    /// itself (a <c>GetXListInput.OfficeId</c> filter, say).
    ///
    /// <list type="bullet">
    /// <item><b>No office context.</b> The caller's own filter is used as it always was, and a
    /// request without one stays unscoped inside its tenant. Nothing about existing clients
    /// changes.</item>
    /// <item><b>A specific office.</b> A caller filter that names the same office is redundant and
    /// accepted; one that names a different office is a <i>conflict</i> and is refused, rather than
    /// silently resolved in favour of either value — the two say different things and only the user
    /// knows which they meant.</item>
    /// <item><b>All offices.</b> A caller filter narrows within the granted scope, which is what a
    /// filter is for; an office outside that scope is refused.</item>
    /// </list>
    ///
    /// <para>The context itself is never re-validated here. It was validated once, during office
    /// resolution, against the caller's permitted offices.</para>
    /// </summary>
    /// <param name="currentOffice">The request's validated office context.</param>
    /// <param name="requestedOfficeId">An office filter carried by the request, or <c>null</c>.</param>
    /// <exception cref="BusinessException">
    /// The request filter and the office context name two different offices (400).
    /// </exception>
    /// <exception cref="EnsaAuthorizationException">
    /// The request filter names an office outside the granted scope (403).
    /// </exception>
    public static OfficeQueryScope Resolve(ICurrentOffice currentOffice, int? requestedOfficeId)
    {
        ArgumentNullException.ThrowIfNull(currentOffice);

        if (!currentOffice.IsSpecified)
        {
            return requestedOfficeId is { } plain
                ? new OfficeQueryScope([plain])
                : OfficeQueryScope.Unrestricted;
        }

        if (currentOffice.HasOffice)
        {
            var selected = currentOffice.CurrentOfficeId!.Value;

            if (requestedOfficeId is { } filtered && filtered != selected)
            {
                throw new BusinessException(
                        "The office filter does not match the office this request is running for.",
                        "Ensa:Office:FilterConflict")
                    .WithData("RequestedOfficeId", filtered)
                    .WithData("CurrentOfficeId", selected);
            }

            return new OfficeQueryScope([selected]);
        }

        // "All offices": an empty id list means the granted scope is the whole tenant.
        if (requestedOfficeId is not { } narrowed)
        {
            return new OfficeQueryScope(currentOffice.OfficeIds);
        }

        if (currentOffice.OfficeIds.Count > 0 && !currentOffice.OfficeIds.Contains(narrowed))
        {
            throw new EnsaAuthorizationException(
                "You are not allowed to work in the selected office.",
                "Ensa:Office:NotPermitted");
        }

        return new OfficeQueryScope([narrowed]);
    }
}
