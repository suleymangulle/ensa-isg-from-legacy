using Ensa.Domain.Common;
using Ensa.Domain.Tenancy;

namespace Ensa.HttpApi.Host.Ambient;

/// <summary>
/// <see cref="ICurrentOffice"/> for one HTTP request.
/// <para>
/// Registered <b>scoped</b>, so the instance the middleware fills in is the same one every service
/// in that request reads. That is deliberately not the <see cref="AsyncLocal{T}"/> arrangement the
/// tenant uses: the tenant context has to survive being changed and restored inside a request
/// (<c>ICurrentTenant.Change</c>, used by host administration screens), while the office context is
/// resolved once, at the edge, and never moves. A scoped object says that plainly and cannot leak
/// between requests at all.
/// </para>
/// <para>
/// Its default is <see cref="ResolvedOfficeContext.None"/> — no office context — so a request that
/// never reaches the middleware (a unit test, a background call) behaves exactly as it did before
/// the office context existed rather than failing or, worse, being scoped to something arbitrary.
/// </para>
/// </summary>
public sealed class RequestOffice : ICurrentOffice
{
    private ResolvedOfficeContext _context = ResolvedOfficeContext.None;

    /// <inheritdoc />
    public bool IsSpecified => _context.IsSpecified;

    /// <inheritdoc />
    public bool HasOffice => _context.HasOffice;

    /// <inheritdoc />
    public int? CurrentOfficeId => _context.OfficeId;

    /// <inheritdoc />
    public bool IsAllOffices => _context.IsAllOffices;

    /// <inheritdoc />
    public IReadOnlyList<int> OfficeIds => _context.OfficeIds;

    /// <summary>
    /// Installs the validated context. Called by <c>OfficeResolutionMiddleware</c> and by nothing
    /// else — the value it takes has already been checked against the caller's permitted offices,
    /// and there is no second place that is allowed to decide what those are.
    /// </summary>
    public void Set(ResolvedOfficeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }
}
