using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.Domain.Repositories;
using Ensa.Domain.Services;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Tenancy;

/// <summary>What the caller asked for, straight off the <c>X-Ensa-OfficeId</c> header.</summary>
public enum OfficeContextRequestKind
{
    /// <summary>No header was sent. The request runs unscoped inside its tenant.</summary>
    None = 0,

    /// <summary>A single office id was sent.</summary>
    Specific = 1,

    /// <summary>The neutral "all offices" token was sent (the UI's "Tüm Şubeler").</summary>
    AllOffices = 2
}

/// <summary>An office context request, already parsed but not yet validated.</summary>
/// <param name="Kind">Which of the three forms the header took.</param>
/// <param name="OfficeId">The requested office; set only for <see cref="OfficeContextRequestKind.Specific"/>.</param>
public sealed record OfficeContextRequest(OfficeContextRequestKind Kind, int? OfficeId)
{
    /// <summary>No office context was supplied.</summary>
    public static readonly OfficeContextRequest None = new(OfficeContextRequestKind.None, null);

    /// <summary>The caller asked for every office they may use.</summary>
    public static readonly OfficeContextRequest AllOffices = new(OfficeContextRequestKind.AllOffices, null);

    /// <summary>The caller asked for one specific office.</summary>
    public static OfficeContextRequest Specific(int officeId)
        => new(OfficeContextRequestKind.Specific, officeId);
}

/// <summary>
/// The office context of a request, <b>after</b> it has been validated against the caller's own
/// permitted offices. This is what <see cref="ICurrentOffice"/> exposes.
/// </summary>
/// <param name="IsSpecified">Whether the request carried an office context at all.</param>
/// <param name="HasOffice">Whether one specific office was selected.</param>
/// <param name="OfficeId">The selected office; <c>null</c> unless <paramref name="HasOffice"/>.</param>
/// <param name="IsAllOffices">Whether the granted scope is "every office the caller may use".</param>
/// <param name="OfficeIds">
/// The ids a query must be restricted to. Empty means no office predicate — see
/// <see cref="ICurrentOffice.OfficeIds"/>.
/// </param>
public sealed record ResolvedOfficeContext(
    bool IsSpecified,
    bool HasOffice,
    int? OfficeId,
    bool IsAllOffices,
    IReadOnlyList<int> OfficeIds)
{
    /// <summary>No office context. Every query runs exactly as it did before the office existed.</summary>
    public static readonly ResolvedOfficeContext None = new(false, false, null, false, []);
}

/// <summary>
/// What the current user is allowed to do with offices: which ones they may work in, whether they
/// may take the "all offices" scope, and which one the shell should start on.
/// </summary>
/// <param name="Offices">
/// The offices the caller may work in — active, not soft-deleted, inside the current tenant.
/// Ordered by name, so the switcher's list order is stable.
/// </param>
/// <param name="CoversWholeTenant">
/// <c>true</c> when <paramref name="Offices"/> is "every active office of the tenant" rather than a
/// list of individual assignments. It is the difference between filtering on an explicit id list and
/// not filtering at all, because the tenant filter already draws that same boundary.
/// </param>
/// <param name="AllOfficesAllowed">Whether the caller may take the "all offices" scope.</param>
/// <param name="DefaultOfficeId">The office the shell should start on, or <c>null</c>.</param>
public sealed record OfficeAccess(
    IReadOnlyList<Office> Offices,
    bool CoversWholeTenant,
    bool AllOfficesAllowed,
    int? DefaultOfficeId);

/// <summary>
/// The single authority on which offices a user may work in, and on whether the office context a
/// request carries is one they are allowed to have.
/// </summary>
public interface IOfficeAccessManager : IDomainService
{
    /// <summary>
    /// The current user's permitted offices, their "all offices" right and their default office.
    /// Computed once per request and held, so several callers cost one query set.
    /// </summary>
    Task<OfficeAccess> GetAccessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a parsed office context request and turns it into the scope the request will run
    /// under. Throws <see cref="EnsaAuthorizationException"/> (403) when the caller may not have the
    /// office they asked for.
    /// </summary>
    Task<ResolvedOfficeContext> ResolveAsync(
        OfficeContextRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Office access rules, in one place.
///
/// <para><b>Which offices a user may work in.</b> Three cases, in this order:</para>
/// <list type="number">
/// <item>
/// The user has <see cref="UserOffice"/> assignments → exactly those offices (active, not deleted,
/// current tenant). This is the strict rule and it is what closes the legacy hole: the legacy
/// <c>DefaultController.SetOfisId</c> wrote whatever office id it was handed into the session and
/// only ever checked that it belonged to the same <c>KurumId</c>, never that the user was assigned
/// to it.
/// </item>
/// <item>
/// No assignments, and the user is an organization or system administrator → every active office of
/// the tenant. This is the legacy fallback in <c>Businness/Genel/OfisIslemleri.GetOfisler</c>,
/// narrowed to the two roles that legacy actually offered the switcher to: the control rendered
/// only for <c>PersonelTuru == "Admin"</c>, which
/// <see cref="EnsaRoleNames.OrganizationAdministrator"/> is documented as being
/// (legacy <c>Kullanici_T.Admin</c>), and <see cref="EnsaRoleNames.SystemAdministrator"/> is the
/// legacy <c>SerAdmin</c> above it.
/// </item>
/// <item>
/// No assignments, anyone else — including <see cref="EnsaRoleNames.OfficeAdministrator"/> → no
/// offices. Legacy gave an office administrator with no assignment nothing either
/// (<c>Kullanici.OfisId.HasValue ? a.OfisId == Kullanici.OfisId.Value : false</c>), and legacy's
/// remaining "everyone else sees every office" branch is deliberately <b>not</b> reproduced: it was
/// the widest possible default, and it was never reachable through the switcher because the
/// switcher was administrator-only. Such a user simply gets no switcher and no office header, which
/// leaves their requests scoped exactly as they are today — by tenant.
/// </item>
/// </list>
///
/// <para><b>"All offices" (the UI's "Tüm Şubeler").</b> Allowed when the permitted set is the whole
/// tenant (an administrator, as above) or when the user has more than one assignment. For the second
/// case it is a union of that user's own offices, never a tenant-wide scope — so it can never show
/// more than the offices they were already allowed to select one at a time.</para>
///
/// <para><b>The default office.</b> The lowest-id <see cref="UserOffice"/> row wins. That is not an
/// arbitrary tie-break: the data migration writes the legacy per-user default office
/// (<c>Kullanici_T.OfisId</c>) first, in <c>TenancyStep</c>, and the many-to-many
/// <c>KullaniciOfis_T</c> rows afterwards in <c>UserSplitStep</c> with a
/// <c>WHERE NOT EXISTS</c> guard — so for every migrated user the lowest-id assignment <i>is</i>
/// their legacy default office. For accounts created after the migration it is simply their first
/// assignment. When that office is not (or no longer) permitted, a single permitted office is used
/// instead, and otherwise there is no default and the shell starts on "Tüm Şubeler".</para>
/// </summary>
public class OfficeAccessManager(
    IOfficeRepository officeRepository,
    ICurrentUser currentUser)
    : DomainService, IOfficeAccessManager
{
    private OfficeAccess? _access;

    /// <inheritdoc />
    public async Task<OfficeAccess> GetAccessAsync(CancellationToken cancellationToken = default)
        => _access ??= await BuildAccessAsync(cancellationToken);

    private async Task<OfficeAccess> BuildAccessAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return new OfficeAccess([], CoversWholeTenant: false, AllOfficesAllowed: false, DefaultOfficeId: null);
        }

        // Assigned offices first. The repository already applies the tenant and soft-delete filters
        // and drops inactive offices.
        var assigned = await officeRepository.GetUserOfficesAsync(userId, cancellationToken);

        if (assigned.Count > 0)
        {
            var defaultOfficeId = await officeRepository.FindDefaultUserOfficeIdAsync(userId, cancellationToken);

            return new OfficeAccess(
                assigned,
                CoversWholeTenant: false,
                AllOfficesAllowed: assigned.Count > 1,
                DefaultOfficeId: assigned.Exists(o => o.Id == defaultOfficeId)
                    ? defaultOfficeId
                    : assigned.Count == 1 ? assigned[0].Id : null);
        }

        if (!IsTenantWideAdministrator())
        {
            return new OfficeAccess([], CoversWholeTenant: false, AllOfficesAllowed: false, DefaultOfficeId: null);
        }

        var all = await officeRepository.GetListAsync(o => o.IsActive, cancellationToken);
        all.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.CurrentCulture));

        return new OfficeAccess(
            all,
            CoversWholeTenant: true,
            AllOfficesAllowed: true,
            // An administrator has no assignment to start from, so the shell starts on
            // "Tüm Şubeler" — which is where the legacy shell started too, because
            // Kullanici_T.OfisId was normally null for an administrator and the session
            // office fell back to 0, "Tüm Ofisler".
            DefaultOfficeId: all.Count == 1 ? all[0].Id : null);
    }

    private bool IsTenantWideAdministrator()
        => currentUser.IsInRole(EnsaRoleNames.SystemAdministrator)
           || currentUser.IsInRole(EnsaRoleNames.OrganizationAdministrator);

    /// <inheritdoc />
    public async Task<ResolvedOfficeContext> ResolveAsync(
        OfficeContextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Kind == OfficeContextRequestKind.None)
        {
            return ResolvedOfficeContext.None;
        }

        var access = await GetAccessAsync(cancellationToken);

        if (request.Kind == OfficeContextRequestKind.AllOffices)
        {
            if (!access.AllOfficesAllowed)
            {
                throw new EnsaAuthorizationException(
                    "You are not allowed to work across every office.",
                    "Ensa:Office:AllOfficesNotPermitted");
            }

            // A whole-tenant scope needs no predicate: the tenant filter already draws that line,
            // and an IN list of every office would only repeat it less efficiently.
            var ids = access.CoversWholeTenant
                ? (IReadOnlyList<int>)[]
                : [.. access.Offices.Select(o => o.Id)];

            return new ResolvedOfficeContext(
                IsSpecified: true,
                HasOffice: false,
                OfficeId: null,
                IsAllOffices: true,
                OfficeIds: ids);
        }

        var requestedId = request.OfficeId
            ?? throw new ArgumentException("A specific office request must carry an id.", nameof(request));

        // One answer for every way this can fail — it does not exist, it is inactive, it is
        // soft-deleted, it belongs to another tenant, or it is simply not this user's. Telling them
        // apart would tell a caller which office ids exist in tenants they cannot see.
        if (!access.Offices.Any(o => o.Id == requestedId))
        {
            throw new EnsaAuthorizationException(
                "You are not allowed to work in the selected office.",
                "Ensa:Office:NotPermitted");
        }

        return new ResolvedOfficeContext(
            IsSpecified: true,
            HasOffice: true,
            OfficeId: requestedId,
            IsAllOffices: false,
            OfficeIds: [requestedId]);
    }
}
