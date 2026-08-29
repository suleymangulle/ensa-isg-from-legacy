using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore.Ambient;
using Ensa.EntityFrameworkCore.Repositories.Tenancy;
using Ensa.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Tests;

/// <summary>
/// <see cref="OfficeAccessManager"/> — which offices a user may work in, and whether the office
/// context a request carries is one they are allowed to have.
/// <para>
/// Run against a real LocalDB database rather than a stub, for the same reason
/// <c>TenantIsolationTests</c> is: the answer depends on the tenant and soft-delete query filters,
/// and a filter is only worth what SQL Server agrees it is worth. The whole point of this class is
/// that it is the <b>single</b> authority the office switcher and the office header both go
/// through, so if it says yes to something it should not, every office-scoped screen says yes with
/// it.
/// </para>
/// <para>The fixture creates and drops its own database per test class.</para>
/// </summary>
public class OfficeAccessTests : IAsyncLifetime
{
    private const int TenantA = 1;
    private const int TenantB = 2;
    private const int UserId = 1;

    private EnsaTestFixture _fixture = null!;

    public Task InitializeAsync()
    {
        _fixture = new EnsaTestFixture(tenantId: TenantA, userId: UserId, databaseCreate: true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    // ------------------------------------------------------------------ helpers

    private IDisposable AsTenant(int? tenantId)
    {
        var previous = _fixture.TenantAccessor.Current;
        _fixture.TenantAccessor.Current = new TenantInfo(tenantId, "Test");
        return new Restore(() => _fixture.TenantAccessor.Current = previous);
    }

    private sealed class Restore(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    private async Task<int> AddOfficeAsync(string name, bool isActive = true, bool headquarters = false)
    {
        var office = new Office { Name = name, IsActive = isActive, IsHeadquarterOffice = headquarters };

        await using var context = _fixture.CreateContext();
        context.Set<Office>().Add(office);
        await context.SaveChangesAsync();
        return office.Id;
    }

    private async Task AssignAsync(int officeId, int userId = UserId)
    {
        await using var context = _fixture.CreateContext();
        context.Set<UserOffice>().Add(new UserOffice { UserId = userId, OfficeId = officeId });
        await context.SaveChangesAsync();
    }

    private async Task SoftDeleteOfficeAsync(int officeId)
    {
        await using var context = _fixture.CreateContext();
        var office = await context.Set<Office>().FirstAsync(o => o.Id == officeId);
        context.Set<Office>().Remove(office);
        await context.SaveChangesAsync();
    }

    /// <summary>A manager bound to a fresh context, so each assertion sees the database, not a cache.</summary>
    private (OfficeAccessManager Manager, EnsaDbContext Context) NewManager(params string[] roles)
    {
        _fixture.CurrentUser.Roles = roles;

        var context = _fixture.CreateContext();
        var repository = new OfficeRepository(context, _fixture.DataFilter);

        return (new OfficeAccessManager(repository, _fixture.CurrentUser), context);
    }

    // ------------------------------------------------------- the permitted set

    [Fact]
    public async Task Assigned_offices_are_the_permitted_set()
    {
        var kadikoy = await AddOfficeAsync("Kadıköy");
        var ankara = await AddOfficeAsync("Ankara");
        await AddOfficeAsync("Izmir — nobody's");
        await AssignAsync(kadikoy);
        await AssignAsync(ankara);

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([kadikoy, ankara], access.Offices.Select(o => o.Id).Order());
        Assert.False(access.CoversWholeTenant);
    }

    [Fact]
    public async Task An_inactive_office_is_never_offered()
    {
        var live = await AddOfficeAsync("Kadıköy");
        var closed = await AddOfficeAsync("Kapalı", isActive: false);
        await AssignAsync(live);
        await AssignAsync(closed);

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([live], access.Offices.Select(o => o.Id));
    }

    [Fact]
    public async Task A_soft_deleted_office_is_never_offered()
    {
        var live = await AddOfficeAsync("Kadıköy");
        var removed = await AddOfficeAsync("Silinmiş");
        await AssignAsync(live);
        await AssignAsync(removed);
        await SoftDeleteOfficeAsync(removed);

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([live], access.Offices.Select(o => o.Id));
    }

    [Fact]
    public async Task Another_tenants_office_is_never_offered_even_when_assigned()
    {
        int foreignOffice;
        using (AsTenant(TenantB))
        {
            foreignOffice = await AddOfficeAsync("Başka Kurumun Ofisi");
        }

        var own = await AddOfficeAsync("Kadıköy");
        await AssignAsync(own);

        // The assignment row itself is written in tenant A, pointing at tenant B's office — the
        // worst case, because nothing but the tenant filter stands between the two.
        await AssignAsync(foreignOffice);

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([own], access.Offices.Select(o => o.Id));
    }

    [Fact]
    public async Task A_user_with_no_assignment_and_no_administrator_role_gets_no_office()
    {
        await AddOfficeAsync("Kadıköy");
        await AddOfficeAsync("Ankara");

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        // Deliberately narrower than the legacy fallback, which handed every office of the
        // organization to anyone who was not an office administrator.
        Assert.Empty(access.Offices);
        Assert.False(access.AllOfficesAllowed);
        Assert.Null(access.DefaultOfficeId);
    }

    [Fact]
    public async Task An_office_administrator_with_no_assignment_gets_no_office()
    {
        await AddOfficeAsync("Kadıköy");

        var (manager, context) = NewManager(EnsaRoleNames.OfficeAdministrator);
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Empty(access.Offices);
    }

    [Fact]
    public async Task An_organization_administrator_with_no_assignment_gets_every_active_office()
    {
        var kadikoy = await AddOfficeAsync("Kadıköy");
        var ankara = await AddOfficeAsync("Ankara");
        await AddOfficeAsync("Kapalı", isActive: false);

        var (manager, context) = NewManager(EnsaRoleNames.OrganizationAdministrator);
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([kadikoy, ankara], access.Offices.Select(o => o.Id).Order());
        Assert.True(access.CoversWholeTenant);
        Assert.True(access.AllOfficesAllowed);
    }

    // ------------------------------------------------------------ all offices

    [Fact]
    public async Task All_offices_needs_more_than_one_assignment()
    {
        var only = await AddOfficeAsync("Kadıköy");
        await AssignAsync(only);

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.False(access.AllOfficesAllowed);
        Assert.Equal(only, access.DefaultOfficeId);
    }

    [Fact]
    public async Task Two_assignments_allow_all_offices_over_exactly_those_two()
    {
        var kadikoy = await AddOfficeAsync("Kadıköy");
        var ankara = await AddOfficeAsync("Ankara");
        await AddOfficeAsync("Izmir — nobody's");
        await AssignAsync(kadikoy);
        await AssignAsync(ankara);

        var (manager, context) = NewManager();
        await using var _ = context;

        Assert.True((await manager.GetAccessAsync()).AllOfficesAllowed);

        var resolved = await manager.ResolveAsync(OfficeContextRequest.AllOffices);

        Assert.True(resolved.IsAllOffices);
        Assert.False(resolved.HasOffice);
        // Their own two offices, not the tenant: "all" means all of *mine*.
        Assert.Equal([kadikoy, ankara], resolved.OfficeIds.Order());
    }

    [Fact]
    public async Task All_offices_for_an_administrator_carries_no_id_list()
    {
        await AddOfficeAsync("Kadıköy");
        await AddOfficeAsync("Ankara");

        var (manager, context) = NewManager(EnsaRoleNames.OrganizationAdministrator);
        await using var _ = context;

        var resolved = await manager.ResolveAsync(OfficeContextRequest.AllOffices);

        Assert.True(resolved.IsAllOffices);
        // Empty means "no predicate": the tenant filter already draws exactly this line.
        Assert.Empty(resolved.OfficeIds);
    }

    [Fact]
    public async Task All_offices_is_refused_when_it_was_not_granted()
    {
        var only = await AddOfficeAsync("Kadıköy");
        await AssignAsync(only);

        var (manager, context) = NewManager();
        await using var _ = context;

        var exception = await Assert.ThrowsAsync<EnsaAuthorizationException>(
            () => manager.ResolveAsync(OfficeContextRequest.AllOffices));

        Assert.Equal("Ensa:Office:AllOfficesNotPermitted", exception.Code);
    }

    // ------------------------------------------------------- resolving a request

    [Fact]
    public async Task No_header_resolves_to_no_office_context()
    {
        var (manager, context) = NewManager();
        await using var _ = context;

        var resolved = await manager.ResolveAsync(OfficeContextRequest.None);

        Assert.False(resolved.IsSpecified);
        Assert.False(resolved.HasOffice);
        Assert.Empty(resolved.OfficeIds);
    }

    [Fact]
    public async Task An_assigned_office_initialises_the_context()
    {
        var kadikoy = await AddOfficeAsync("Kadıköy");
        await AssignAsync(kadikoy);

        var (manager, context) = NewManager();
        await using var _ = context;

        var resolved = await manager.ResolveAsync(OfficeContextRequest.Specific(kadikoy));

        Assert.True(resolved.IsSpecified);
        Assert.True(resolved.HasOffice);
        Assert.Equal(kadikoy, resolved.OfficeId);
        Assert.Equal([kadikoy], resolved.OfficeIds);
    }

    [Fact]
    public async Task An_unassigned_office_is_refused()
    {
        var mine = await AddOfficeAsync("Kadıköy");
        var someone_elses = await AddOfficeAsync("Ankara");
        await AssignAsync(mine);

        var (manager, context) = NewManager();
        await using var _ = context;

        var exception = await Assert.ThrowsAsync<EnsaAuthorizationException>(
            () => manager.ResolveAsync(OfficeContextRequest.Specific(someone_elses)));

        Assert.Equal("Ensa:Office:NotPermitted", exception.Code);
    }

    [Fact]
    public async Task An_inactive_office_is_refused()
    {
        var closed = await AddOfficeAsync("Kapalı", isActive: false);
        await AssignAsync(closed);

        var (manager, context) = NewManager();
        await using var _ = context;

        await Assert.ThrowsAsync<EnsaAuthorizationException>(
            () => manager.ResolveAsync(OfficeContextRequest.Specific(closed)));
    }

    [Fact]
    public async Task A_non_existent_office_is_refused_the_same_way_an_unassigned_one_is()
    {
        var mine = await AddOfficeAsync("Kadıköy");
        await AssignAsync(mine);

        var (manager, context) = NewManager();
        await using var _ = context;

        var exception = await Assert.ThrowsAsync<EnsaAuthorizationException>(
            () => manager.ResolveAsync(OfficeContextRequest.Specific(999_999)));

        // Same code and same message as "not yours": telling the two apart would let a caller map
        // out which office ids exist, including in tenants they cannot see.
        Assert.Equal("Ensa:Office:NotPermitted", exception.Code);
    }

    [Fact]
    public async Task Another_tenants_office_is_refused_without_disclosing_that_it_exists()
    {
        int foreignOffice;
        using (AsTenant(TenantB))
        {
            foreignOffice = await AddOfficeAsync("Başka Kurumun Ofisi");
        }

        var mine = await AddOfficeAsync("Kadıköy");
        await AssignAsync(mine);

        var (manager, context) = NewManager(EnsaRoleNames.OrganizationAdministrator);
        await using var _ = context;

        var exception = await Assert.ThrowsAsync<EnsaAuthorizationException>(
            () => manager.ResolveAsync(OfficeContextRequest.Specific(foreignOffice)));

        Assert.Equal("Ensa:Office:NotPermitted", exception.Code);
    }

    [Fact]
    public async Task Resolving_an_office_never_changes_the_tenant()
    {
        var kadikoy = await AddOfficeAsync("Kadıköy");
        await AssignAsync(kadikoy);

        var (manager, context) = NewManager();
        await using var _ = context;

        var before = _fixture.TenantAccessor.Current;

        await manager.ResolveAsync(OfficeContextRequest.Specific(kadikoy));

        Assert.Equal(before, _fixture.TenantAccessor.Current);
        Assert.Equal(TenantA, _fixture.TenantAccessor.Current?.TenantId);
    }

    // --------------------------------------------------------- the default office

    [Fact]
    public async Task The_default_is_the_first_assignment_which_is_the_legacy_default_office()
    {
        // The migration writes Kullanici_T.OfisId as an assignment first (TenancyStep) and the
        // KullaniciOfis_T rows afterwards (UserSplitStep), so the lowest-id assignment is the
        // legacy per-user default office. Named alphabetically last on purpose, so a default
        // resolved by name rather than by assignment order would fail here.
        var legacyDefault = await AddOfficeAsync("Zonguldak");
        var later = await AddOfficeAsync("Ankara");

        await AssignAsync(legacyDefault);
        await AssignAsync(later);

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal(legacyDefault, access.DefaultOfficeId);
    }

    [Fact]
    public async Task No_default_is_offered_when_the_first_assignment_is_no_longer_usable()
    {
        var closed = await AddOfficeAsync("Kapalı", isActive: false);
        var one = await AddOfficeAsync("Ankara");
        var two = await AddOfficeAsync("Kadıköy");

        await AssignAsync(closed);
        await AssignAsync(one);
        await AssignAsync(two);

        var (manager, context) = NewManager();
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        // Two usable offices and no proven preference between them: the shell starts on
        // "Tüm Şubeler" rather than picking one at random.
        Assert.Null(access.DefaultOfficeId);
        Assert.True(access.AllOfficesAllowed);
    }

    [Fact]
    public async Task An_administrator_starts_on_all_offices_rather_than_one()
    {
        await AddOfficeAsync("Kadıköy");
        await AddOfficeAsync("Ankara");

        var (manager, context) = NewManager(EnsaRoleNames.OrganizationAdministrator);
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Null(access.DefaultOfficeId);
        Assert.True(access.AllOfficesAllowed);
    }

    [Fact]
    public async Task A_single_office_is_the_default_even_for_an_administrator()
    {
        var only = await AddOfficeAsync("Kadıköy");

        var (manager, context) = NewManager(EnsaRoleNames.OrganizationAdministrator);
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal(only, access.DefaultOfficeId);
    }
}
