using Ensa.Domain.Common;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Tenancy;

namespace Ensa.Application.Tests;

/// <summary>
/// <see cref="OfficeScope.Resolve"/> — the reconciliation between the office a request is running
/// for and an office filter the caller also put in the request.
/// <para>
/// This is the rule every office-scoped query goes through, so it is worth pinning on its own: the
/// alternative is discovering in a screen that two office values disagreed and one of them silently
/// won. No database is needed — the office context reaching this point has already been validated.
/// </para>
/// </summary>
public class OfficeScopeTests
{
    /// <summary>An office context, as office resolution would have left it.</summary>
    private sealed class StubOffice : ICurrentOffice
    {
        public bool IsSpecified { get; init; }
        public bool HasOffice { get; init; }
        public int? CurrentOfficeId { get; init; }
        public bool IsAllOffices { get; init; }
        public IReadOnlyList<int> OfficeIds { get; init; } = [];

        public static StubOffice None => new();

        public static StubOffice Single(int officeId) => new()
        {
            IsSpecified = true,
            HasOffice = true,
            CurrentOfficeId = officeId,
            OfficeIds = [officeId]
        };

        /// <summary>"All offices" for a user whose permitted set is a list of assignments.</summary>
        public static StubOffice AllOf(params int[] officeIds) => new()
        {
            IsSpecified = true,
            IsAllOffices = true,
            OfficeIds = officeIds
        };

        /// <summary>"All offices" for an administrator, whose scope is the whole tenant.</summary>
        public static StubOffice AllTenantWide => new()
        {
            IsSpecified = true,
            IsAllOffices = true,
            OfficeIds = []
        };
    }

    // ---------------------------------------------------------- no office context

    [Fact]
    public void No_office_context_and_no_filter_leaves_the_query_unrestricted()
    {
        var scope = OfficeScope.Resolve(StubOffice.None, requestedOfficeId: null);

        Assert.False(scope.IsRestricted);
        Assert.Null(scope.SingleOfficeId);
        Assert.Empty(scope.OfficeIds);
    }

    [Fact]
    public void No_office_context_keeps_honouring_the_callers_own_filter()
    {
        // A client that never sends the header behaves exactly as it did before offices existed.
        var scope = OfficeScope.Resolve(StubOffice.None, requestedOfficeId: 7);

        Assert.True(scope.IsRestricted);
        Assert.Equal(7, scope.SingleOfficeId);
    }

    // ------------------------------------------------------------ one office

    [Fact]
    public void A_selected_office_restricts_the_query_to_it()
    {
        var scope = OfficeScope.Resolve(StubOffice.Single(4), requestedOfficeId: null);

        Assert.Equal(4, scope.SingleOfficeId);
        Assert.Equal([4], scope.OfficeIds);
    }

    [Fact]
    public void A_filter_naming_the_selected_office_is_redundant_but_accepted()
    {
        var scope = OfficeScope.Resolve(StubOffice.Single(4), requestedOfficeId: 4);

        Assert.Equal(4, scope.SingleOfficeId);
    }

    [Fact]
    public void A_filter_contradicting_the_selected_office_is_refused()
    {
        // Neither value may quietly win: they say different things and only the user knows which
        // they meant.
        var exception = Assert.Throws<BusinessException>(
            () => OfficeScope.Resolve(StubOffice.Single(4), requestedOfficeId: 9));

        Assert.Equal("Ensa:Office:FilterConflict", exception.Code);
        Assert.IsNotType<EnsaAuthorizationException>(exception);
    }

    // ------------------------------------------------------------ all offices

    [Fact]
    public void All_offices_over_assignments_restricts_to_those_assignments()
    {
        var scope = OfficeScope.Resolve(StubOffice.AllOf(2, 5), requestedOfficeId: null);

        Assert.True(scope.IsRestricted);
        Assert.Equal([2, 5], scope.OfficeIds);
        Assert.Null(scope.SingleOfficeId);
    }

    [Fact]
    public void All_offices_for_an_administrator_needs_no_predicate()
    {
        // The permitted set is the whole tenant, and the tenant filter already draws that line.
        var scope = OfficeScope.Resolve(StubOffice.AllTenantWide, requestedOfficeId: null);

        Assert.False(scope.IsRestricted);
    }

    [Fact]
    public void A_filter_narrows_within_the_all_offices_scope()
    {
        var scope = OfficeScope.Resolve(StubOffice.AllOf(2, 5), requestedOfficeId: 5);

        Assert.Equal(5, scope.SingleOfficeId);
    }

    [Fact]
    public void A_filter_outside_the_all_offices_scope_is_refused()
    {
        var exception = Assert.Throws<EnsaAuthorizationException>(
            () => OfficeScope.Resolve(StubOffice.AllOf(2, 5), requestedOfficeId: 9));

        Assert.Equal("Ensa:Office:NotPermitted", exception.Code);
    }

    [Fact]
    public void A_filter_under_a_tenant_wide_scope_is_accepted()
    {
        // Any office of the tenant is inside an administrator's scope, and the tenant filter is what
        // proves the office is in this tenant at all.
        var scope = OfficeScope.Resolve(StubOffice.AllTenantWide, requestedOfficeId: 9);

        Assert.Equal(9, scope.SingleOfficeId);
    }
}
