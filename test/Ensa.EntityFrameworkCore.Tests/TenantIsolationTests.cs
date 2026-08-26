using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Shared.Enums;
using Ensa.EntityFrameworkCore.Ambient;
using Ensa.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Tests;

/// <summary>
/// Integration tests for the global query filters, against a real LocalDB database.
/// <para>
/// <c>ModelValidationTests</c> asserts that a filter <i>exists</i> on every tenant-scoped entity.
/// That is a statement about metadata; it cannot tell whether the filter actually keeps one
/// customer's rows away from another. In a multi-tenant OHS system that difference is the
/// difference between a working product and a cross-customer data breach, so it is proved here
/// by writing rows as one tenant and reading them as another.
/// </para>
/// <para>
/// The fixture creates and drops its own database per test class, so these tests do not touch
/// the development database.
/// </para>
/// </summary>
public class TenantIsolationTests : IAsyncLifetime
{
    private const int TenantA = 1;
    private const int TenantB = 2;

    private EnsaTestFixture _fixture = null!;

    public Task InitializeAsync()
    {
        _fixture = new EnsaTestFixture(tenantId: TenantA, userId: 1, databaseCreate: true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    // ------------------------------------------------------------------ helpers

    /// <summary>Runs the enclosing block as another tenant, restoring the previous one after.</summary>
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

    private static Company NewCompany(string name) => new()
    {
        CompanyName = name,
        HazardClass = HazardClass.Hazardous,
        WorkplaceType = WorkplaceType.Headquarter,
        CityId = 34,
        DistrictId = 1,
        IsActive = true
    };

    private async Task<int> InsertAsync(Company company)
    {
        await using var context = _fixture.CreateContext();
        context.Set<Company>().Add(company);
        await context.SaveChangesAsync();
        return company.Id;
    }

    // ------------------------------------------------------------------ tests

    [Fact]
    public async Task Stamps_the_current_tenant_on_insert()
    {
        var id = await InsertAsync(NewCompany("Tenant A Ltd"));

        await using var context = _fixture.CreateContext();
        var stored = await context.Set<Company>().SingleAsync(c => c.Id == id);

        Assert.Equal(TenantA, stored.TenantId);
    }

    [Fact]
    public async Task One_tenant_cannot_read_another_tenants_rows()
    {
        var id = await InsertAsync(NewCompany("Tenant A Ltd"));

        using (AsTenant(TenantB))
        {
            await using var context = _fixture.CreateContext();

            Assert.Null(await context.Set<Company>().FirstOrDefaultAsync(c => c.Id == id));
            Assert.Empty(await context.Set<Company>().ToListAsync());
        }

        // ...and the row is still there for the tenant that owns it.
        await using var owner = _fixture.CreateContext();
        Assert.NotNull(await owner.Set<Company>().FirstOrDefaultAsync(c => c.Id == id));
    }

    [Fact]
    public async Task A_host_row_is_visible_to_every_tenant()
    {
        int id;

        using (AsTenant(null))
        {
            // TenantId stays null, which is what marks the row as shared host data.
            id = await InsertAsync(NewCompany("Host Reference Ltd"));
        }

        foreach (var tenantId in new[] { TenantA, TenantB })
        {
            using (AsTenant(tenantId))
            {
                await using var context = _fixture.CreateContext();
                Assert.NotNull(await context.Set<Company>().FirstOrDefaultAsync(c => c.Id == id));
            }
        }
    }

    [Fact]
    public async Task Disabling_the_tenant_filter_reveals_every_tenants_rows_and_restores_afterwards()
    {
        var idA = await InsertAsync(NewCompany("Tenant A Ltd"));

        int idB;
        using (AsTenant(TenantB))
        {
            idB = await InsertAsync(NewCompany("Tenant B Ltd"));
        }

        await using var context = _fixture.CreateContext();

        // The sign-in path needs this, because a user must be found before the tenant is known
        // (see ADR-011). It is the one deliberate hole in the isolation, so it has to close again.
        using (_fixture.DataFilter.Disable<IMultiTenant>())
        {
            var all = await context.Set<Company>().Select(c => c.Id).ToListAsync();
            Assert.Contains(idA, all);
            Assert.Contains(idB, all);
        }

        var afterDispose = await context.Set<Company>().Select(c => c.Id).ToListAsync();
        Assert.Contains(idA, afterDispose);
        Assert.DoesNotContain(idB, afterDispose);
    }

    [Fact]
    public async Task A_soft_deleted_row_disappears_from_queries_but_stays_in_the_table()
    {
        var id = await InsertAsync(NewCompany("To Be Deleted Ltd"));

        await using (var deleting = _fixture.CreateContext())
        {
            var company = await deleting.Set<Company>().SingleAsync(c => c.Id == id);
            deleting.Set<Company>().Remove(company);
            await deleting.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();

        Assert.Null(await context.Set<Company>().FirstOrDefaultAsync(c => c.Id == id));

        // Soft delete must not be a physical delete: the row is still there, flagged.
        using (_fixture.DataFilter.Disable<ISoftDelete>())
        {
            var deleted = await context.Set<Company>().FirstOrDefaultAsync(c => c.Id == id);
            Assert.NotNull(deleted);
            Assert.True(deleted!.IsDeleted);
            Assert.NotNull(deleted.DeletionTime);
        }
    }

    [Fact]
    public async Task Disabling_soft_delete_does_not_disable_tenant_isolation()
    {
        int idB;
        using (AsTenant(TenantB))
        {
            idB = await InsertAsync(NewCompany("Tenant B Ltd"));
        }

        await using var context = _fixture.CreateContext();

        // The two filters are independent; turning one off must not widen the other.
        using (_fixture.DataFilter.Disable<ISoftDelete>())
        {
            Assert.Null(await context.Set<Company>().FirstOrDefaultAsync(c => c.Id == idB));
        }
    }
}
