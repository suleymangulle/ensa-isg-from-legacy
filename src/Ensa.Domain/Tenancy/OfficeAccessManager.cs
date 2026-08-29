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
/// <para><b>Company-bound users have no office context at all.</b> A customer contact's working
/// scope is their own workplace, which the company-scope filter already enforces on every query.
/// Offering them an office to switch to would be meaningless, and an office header must never
/// become a way around that filter — so they are answered with an empty set before anything else is
/// asked. It is belt and braces: the global filter would refuse to widen them anyway.</para>
///
/// <para><b>Everyone else.</b> The permitted set is the union of two things:</para>
/// <list type="number">
/// <item>
/// The user's explicit <see cref="UserOffice"/> assignments (active, not deleted, current tenant).
/// This is what closes the legacy hole: <c>DefaultController.SetOfisId</c> wrote whatever office id
/// it was handed into the session and only ever checked that it belonged to the same
/// <c>KurumId</c>, never that the user was assigned to it.
/// </item>
/// <item>
/// Every active office of the tenant, <b>if</b> the user holds
/// <see cref="EnsaRoleNames.OrganizationAdministrator"/> or
/// <see cref="EnsaRoleNames.SystemAdministrator"/>.
/// </item>
/// </list>
///
/// <para><b>Why an administrator gets the whole tenant.</b> Because legacy did, and because the
/// migrated assignment rows cannot say otherwise. Legacy's
/// <c>Businness/Genel/OfisIslemleri.GetOfisler</c> returned every active office of the organization
/// to any user who had no <c>KullaniciOfis_T</c> row and was not an office administrator — and 678
/// of the 766 legacy <c>PersonelTuru == "Admin"</c> accounts had no such row. The migration then
/// wrote each user's legacy <i>default</i> office (<c>Kullanici_T.OfisId</c>) into
/// <see cref="UserOffice"/> as though it were an assignment, so "one row" no longer distinguishes
/// "assigned to exactly one office" from "assigned to none, defaulted to one". Reading that single
/// row as the whole permitted set would silently take nineteen offices away from an administrator
/// who had all twenty. The remaining 88 administrators — the ones who did have explicit legacy
/// assignments — are widened by this rule rather than narrowed, which grants them nothing they could
/// not already reach: within a tenant an office is a <i>filter</i>, not a boundary. A request with no
/// office context already sees every company of the tenant, and selecting an office can only narrow
/// that. The boundaries are the tenant and, for customers, the company.
/// </para>
///
/// <para><b>An office administrator with no assignment gets nothing</b>, which is what legacy gave
/// them too (<c>Kullanici.OfisId.HasValue ? a.OfisId == Kullanici.OfisId.Value : false</c>), and so
/// does anyone else with no assignment and no administrator role. Such a user sends no office header
/// and their requests stay scoped by tenant — the same rows legacy's "Tüm Ofisler" showed them.</para>
///
/// <para><b>"All offices" (the UI's "Tüm Şubeler")</b> is allowed once the permitted set holds more
/// than one office, and to anyone whose scope is the whole tenant. When the scope is the whole
/// tenant it needs no query predicate at all, because the tenant filter already draws exactly that
/// line — which is also why a tenant-wide administrator may take it with an empty list, as a host
/// administrator outside any tenant does: the result is identical to sending no header. Otherwise it
/// is the union of the user's own offices and can never show more than they could select one at a
/// time.</para>
///
/// <para><b>The default office.</b> The lowest-id <see cref="UserOffice"/> row wins. That is not an
/// arbitrary tie-break: the data migration writes the legacy per-user default office
/// (<c>Kullanici_T.OfisId</c>) first, in <c>TenancyStep</c>, and the many-to-many
/// <c>KullaniciOfis_T</c> rows afterwards in <c>UserSplitStep</c> with a
/// <c>WHERE NOT EXISTS</c> guard — so for every migrated user the lowest-id assignment <i>is</i>
/// their legacy default office, and the shell opens where legacy opened. For accounts created after
/// the migration it is simply their first assignment. When that office is not (or no longer)
/// permitted, a single permitted office is used instead, and otherwise there is no default and the
/// shell starts on "Tüm Şubeler".</para>
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

    private static readonly OfficeAccess NoOffices =
        new([], CoversWholeTenant: false, AllOfficesAllowed: false, DefaultOfficeId: null);

    private async Task<OfficeAccess> BuildAccessAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return NoOffices;
        }

        // A customer contact is scoped to their workplace, not to an office. Answering before the
        // repository is asked keeps the switcher off their shell and keeps an office header from
        // ever being a route around the company scope.
        if (currentUser.CompanyId is not null)
        {
            return NoOffices;
        }

        // The repository already applies the tenant and soft-delete filters and drops inactive
        // offices, so everything below is inside the caller's own organization by construction.
        var assigned = await officeRepository.GetUserOfficesAsync(userId, cancellationToken);

        var coversWholeTenant = IsTenantWideAdministrator();

        var permitted = assigned;

        if (coversWholeTenant)
        {
            var all = await officeRepository.GetListAsync(o => o.IsActive, cancellationToken);

            // A union rather than a replacement: an assignment to an office that is somehow not in
            // the active list must not vanish just because the administrator branch ran.
            var seen = new HashSet<int>(all.Select(o => o.Id));
            all.AddRange(assigned.Where(office => seen.Add(office.Id)));

            permitted = all;
        }

        permitted.Sort(static (left, right) =>
            string.Compare(left.Name, right.Name, StringComparison.CurrentCulture));

        // A whole-tenant scope may take "all offices" even when the list is empty, and that is not
        // a loophole: it resolves to no office predicate at all, which is exactly what a request
        // with no header already does. A host administrator working outside any tenant is the case
        // that reaches this — there are no offices in the host context to enumerate — and refusing
        // them a scope identical to sending nothing would be a rule with no effect but a 403.
        var allOfficesAllowed = coversWholeTenant || permitted.Count > 1;

        if (permitted.Count == 0)
        {
            return NoOffices with { CoversWholeTenant = coversWholeTenant, AllOfficesAllowed = allOfficesAllowed };
        }

        var defaultOfficeId = await officeRepository.FindDefaultUserOfficeIdAsync(userId, cancellationToken);

        return new OfficeAccess(
            permitted,
            coversWholeTenant,
            allOfficesAllowed,
            DefaultOfficeId: permitted.Exists(o => o.Id == defaultOfficeId)
                ? defaultOfficeId
                : permitted.Count == 1 ? permitted[0].Id : null);
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
