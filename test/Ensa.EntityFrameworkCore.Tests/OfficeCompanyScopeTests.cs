using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore.Ambient;
using Ensa.EntityFrameworkCore.Repositories.Tenancy;
using Ensa.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Tests;

/// <summary>
/// What a user may see once the company scope and the office scope are both in play.
///
/// <para>
/// These exist because the two scopes were quietly fighting each other. <see cref="Office"/> used to
/// implement <see cref="ICompanyScoped"/>, and because every office in the migrated data has a null
/// <c>CompanyId</c> the fail-closed company filter hid <b>every</b> office from anyone bound to a
/// workplace — including the offices they were assigned to. A user then had a shell that could not
/// name the office it was working in, and 983 members of staff who were company-bound by a migration
/// defect could see exactly one company.
/// </para>
///
/// <para>
/// Against a real LocalDB database, like the other scope tests: a query filter is worth what SQL
/// Server agrees it is worth, not what the model says.
/// </para>
/// </summary>
public class OfficeCompanyScopeTests : IAsyncLifetime
{
    private const int TenantA = 1;
    private const int TenantB = 2;
    private const int StaffUser = 1;
    private const int CustomerUser = 2;

    private EnsaTestFixture _fixture = null!;

    public Task InitializeAsync()
    {
        _fixture = new EnsaTestFixture(tenantId: TenantA, userId: StaffUser, databaseCreate: true);
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

    /// <summary>Binds the ambient user to a workplace, as a customer contact's token does.</summary>
    private IDisposable AsCompanyUser(int? companyId)
    {
        var previous = _fixture.CurrentUser.CompanyId;
        _fixture.CurrentUser.CompanyId = companyId;
        return new Restore(() => _fixture.CurrentUser.CompanyId = previous);
    }

    private sealed class Restore(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    private async Task<int> AddOfficeAsync(string name, bool isActive = true, int? companyId = null)
    {
        var office = new Office { Name = name, IsActive = isActive, CompanyId = companyId };

        await using var context = _fixture.CreateContext();
        context.Set<Office>().Add(office);
        await context.SaveChangesAsync();
        return office.Id;
    }

    private async Task<int> AddCompanyAsync(string name, int officeId)
    {
        var company = new Company
        {
            CompanyName = name,
            HazardClass = HazardClass.Hazardous,
            WorkplaceType = WorkplaceType.Headquarter,
            CityId = 34,
            DistrictId = 1,
            OfficeId = officeId,
            IsActive = true
        };

        await using var context = _fixture.CreateContext();
        context.Set<Company>().Add(company);
        await context.SaveChangesAsync();
        return company.Id;
    }

    private async Task AssignAsync(int officeId, int userId = StaffUser)
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

    private (OfficeAccessManager Manager, EnsaDbContext Context) NewManager(int userId, params string[] roles)
    {
        _fixture.CurrentUser.Roles = roles;

        var context = _fixture.CreateContext();
        return (new OfficeAccessManager(new OfficeRepository(context, _fixture.DataFilter), Stub(userId)), context);
    }

    /// <summary>The fixture's user with an id of our choosing, so a customer and staff can differ.</summary>
    private TestCurrentUser Stub(int userId)
        => userId == StaffUser
            ? _fixture.CurrentUser
            : new TestCurrentUser(userId, TenantA, _fixture.CurrentUser.CompanyId)
            {
                Roles = _fixture.CurrentUser.Roles
            };

    // -------------------------------------------- offices survive the company scope

    [Fact]
    public async Task An_office_is_visible_to_a_user_bound_to_a_workplace()
    {
        // The regression this whole class exists for: Office was ICompanyScoped, every office has a
        // null CompanyId, and the filter fails closed — so a company-bound user saw no offices at
        // all and the shell could not name the office it was in.
        var office = await AddOfficeAsync("Kadıköy");
        var company = await AddCompanyAsync("Bir İşyeri", office);

        using (AsCompanyUser(company))
        {
            await using var context = _fixture.CreateContext();

            var offices = await context.Set<Office>().ToListAsync();

            Assert.Contains(offices, o => o.Id == office);
        }
    }

    [Fact]
    public async Task A_workplace_bound_user_still_sees_only_their_own_workplace()
    {
        // Removing the office marker must not have loosened the company scope itself.
        var office = await AddOfficeAsync("Kadıköy");
        var mine = await AddCompanyAsync("Benim İşyerim", office);
        var theirs = await AddCompanyAsync("Başkasının İşyeri", office);

        using (AsCompanyUser(mine))
        {
            await using var context = _fixture.CreateContext();

            var companies = await context.Set<Company>().Select(c => c.Id).ToListAsync();

            Assert.Equal([mine], companies);
            Assert.DoesNotContain(theirs, companies);
        }
    }

    [Fact]
    public async Task Another_tenants_office_stays_invisible()
    {
        int foreign;
        using (AsTenant(TenantB))
        {
            foreign = await AddOfficeAsync("Başka Kurumun Ofisi");
        }

        var own = await AddOfficeAsync("Kadıköy");

        await using var context = _fixture.CreateContext();
        var offices = await context.Set<Office>().Select(o => o.Id).ToListAsync();

        Assert.Contains(own, offices);
        Assert.DoesNotContain(foreign, offices);
    }

    [Fact]
    public async Task Inactive_and_soft_deleted_offices_stay_out_of_the_permitted_set()
    {
        var live = await AddOfficeAsync("Kadıköy");
        var closed = await AddOfficeAsync("Kapalı", isActive: false);
        var removed = await AddOfficeAsync("Silinmiş");

        await AssignAsync(live);
        await AssignAsync(closed);
        await AssignAsync(removed);
        await SoftDeleteOfficeAsync(removed);

        var (manager, context) = NewManager(StaffUser);
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([live], access.Offices.Select(o => o.Id));
    }

    // ------------------------------------------------------ who may use which office

    [Fact]
    public async Task An_organization_administrator_reaches_every_office_of_the_tenant()
    {
        // 678 of the 766 legacy administrators had no explicit KullaniciOfis_T row and legacy gave
        // them every office of the organization. The migration wrote their single default office as
        // if it were an assignment, so reading one row as the whole permitted set would take the
        // rest away from them.
        var istanbul = await AddOfficeAsync("Istanbul");
        var ankara = await AddOfficeAsync("Ankara");
        var konya = await AddOfficeAsync("Konya");

        await AssignAsync(istanbul);

        var (manager, context) = NewManager(StaffUser, EnsaRoleNames.OrganizationAdministrator);
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([istanbul, ankara, konya], access.Offices.Select(o => o.Id).Order());
        Assert.True(access.CoversWholeTenant);
        Assert.True(access.AllOfficesAllowed);

        // The office they were defaulted to is still where the shell opens.
        Assert.Equal(istanbul, access.DefaultOfficeId);
    }

    [Fact]
    public async Task A_specialist_reaches_only_the_offices_they_are_assigned_to()
    {
        var istanbul = await AddOfficeAsync("Istanbul");
        var ankara = await AddOfficeAsync("Ankara");
        await AddOfficeAsync("Konya");

        await AssignAsync(istanbul);
        await AssignAsync(ankara);

        var (manager, context) = NewManager(StaffUser);
        await using var _ = context;

        var access = await manager.GetAccessAsync();

        Assert.Equal([istanbul, ankara], access.Offices.Select(o => o.Id).Order());
        Assert.False(access.CoversWholeTenant);
        Assert.True(access.AllOfficesAllowed);
    }

    [Fact]
    public async Task All_offices_covers_only_the_offices_the_user_is_authorized_for()
    {
        var istanbul = await AddOfficeAsync("Istanbul");
        var ankara = await AddOfficeAsync("Ankara");
        var konya = await AddOfficeAsync("Konya");

        await AssignAsync(istanbul);
        await AssignAsync(ankara);

        var (manager, context) = NewManager(StaffUser);
        await using var _ = context;

        var resolved = await manager.ResolveAsync(OfficeContextRequest.AllOffices);

        Assert.True(resolved.IsAllOffices);
        Assert.Equal([istanbul, ankara], resolved.OfficeIds.Order());
        Assert.DoesNotContain(konya, resolved.OfficeIds);
    }

    [Fact]
    public async Task A_customer_contact_is_offered_no_office_at_all()
    {
        // Their scope is the workplace. An office to switch to would be meaningless, and the
        // switcher must not appear.
        var office = await AddOfficeAsync("Kadıköy");
        var company = await AddCompanyAsync("Bir İşyeri", office);
        await AssignAsync(office, CustomerUser);

        using (AsCompanyUser(company))
        {
            var (manager, context) = NewManager(CustomerUser);
            await using var _ = context;

            var access = await manager.GetAccessAsync();

            Assert.Empty(access.Offices);
            Assert.False(access.AllOfficesAllowed);
            Assert.Null(access.DefaultOfficeId);
        }
    }

    [Fact]
    public async Task A_customer_contact_cannot_take_an_office_context()
    {
        // The header is refused outright, so it never even reaches the query where the company
        // filter would have stopped it anyway.
        var office = await AddOfficeAsync("Kadıköy");
        var company = await AddCompanyAsync("Bir İşyeri", office);
        await AssignAsync(office, CustomerUser);

        using (AsCompanyUser(company))
        {
            var (manager, context) = NewManager(CustomerUser);
            await using var _ = context;

            await Assert.ThrowsAsync<Ensa.Domain.Shared.Exceptions.EnsaAuthorizationException>(
                () => manager.ResolveAsync(OfficeContextRequest.Specific(office)));
        }
    }

    [Fact]
    public async Task An_office_header_cannot_widen_a_customer_beyond_their_workplace()
    {
        // Belt and braces: even if a scope were somehow granted, the company filter still stands
        // between the caller and another workplace's rows.
        var office = await AddOfficeAsync("Kadıköy");
        var mine = await AddCompanyAsync("Benim İşyerim", office);
        var theirs = await AddCompanyAsync("Başkasının İşyeri", office);

        using (AsCompanyUser(mine))
        {
            await using var context = _fixture.CreateContext();

            var inThatOffice = await context.Set<Company>()
                .Where(c => c.OfficeId == office)
                .Select(c => c.Id)
                .ToListAsync();

            Assert.Equal([mine], inThatOffice);
            Assert.DoesNotContain(theirs, inThatOffice);
        }
    }

    [Fact]
    public async Task An_unassigned_office_is_refused_for_staff()
    {
        var mine = await AddOfficeAsync("Istanbul");
        var other = await AddOfficeAsync("Ankara");
        await AssignAsync(mine);

        var (manager, context) = NewManager(StaffUser);
        await using var _ = context;

        var exception = await Assert.ThrowsAsync<Ensa.Domain.Shared.Exceptions.EnsaAuthorizationException>(
            () => manager.ResolveAsync(OfficeContextRequest.Specific(other)));

        Assert.Equal("Ensa:Office:NotPermitted", exception.Code);
    }

    [Fact]
    public async Task A_cross_tenant_office_cannot_be_taken_even_by_an_administrator()
    {
        int foreign;
        using (AsTenant(TenantB))
        {
            foreign = await AddOfficeAsync("Başka Kurumun Ofisi");
        }

        await AddOfficeAsync("Istanbul");

        var (manager, context) = NewManager(StaffUser, EnsaRoleNames.OrganizationAdministrator);
        await using var _ = context;

        var exception = await Assert.ThrowsAsync<Ensa.Domain.Shared.Exceptions.EnsaAuthorizationException>(
            () => manager.ResolveAsync(OfficeContextRequest.Specific(foreign)));

        Assert.Equal("Ensa:Office:NotPermitted", exception.Code);
    }

    [Fact]
    public async Task Selecting_an_office_returns_only_that_offices_companies()
    {
        var istanbul = await AddOfficeAsync("Istanbul");
        var ankara = await AddOfficeAsync("Ankara");

        var inIstanbul = await AddCompanyAsync("Istanbul İşyeri", istanbul);
        var inAnkara = await AddCompanyAsync("Ankara İşyeri", ankara);

        await AssignAsync(istanbul);
        await AssignAsync(ankara);

        var (manager, context) = NewManager(StaffUser);
        await using var _ = context;

        var resolved = await manager.ResolveAsync(OfficeContextRequest.Specific(istanbul));
        var scope = OfficeScope.Resolve(new StubOffice(resolved), requestedOfficeId: null);

        var ids = scope.OfficeIds;
        var companies = await context.Set<Company>()
            .Where(c => ids.Contains(c.OfficeId))
            .Select(c => c.Id)
            .ToListAsync();

        Assert.Equal([inIstanbul], companies);
        Assert.DoesNotContain(inAnkara, companies);
    }

    /// <summary>Presents a resolved context as <see cref="ICurrentOffice"/>, as the request does.</summary>
    private sealed class StubOffice(ResolvedOfficeContext context) : ICurrentOffice
    {
        public bool IsSpecified => context.IsSpecified;
        public bool HasOffice => context.HasOffice;
        public int? CurrentOfficeId => context.OfficeId;
        public bool IsAllOffices => context.IsAllOffices;
        public IReadOnlyList<int> OfficeIds => context.OfficeIds;
    }
}
