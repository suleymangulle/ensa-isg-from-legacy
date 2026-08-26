using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Shared.Enums;
using Ensa.EntityFrameworkCore;
using Ensa.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Tests;

/// <summary>
/// The company-scope global query filter (ADR-034).
/// <para>
/// Tenancy separates one OHS provider from another. It says nothing about the customers inside a
/// provider: without this filter a customer contact could list every company their provider
/// serves and read those companies' employees. These tests run against a real LocalDB database,
/// because a query filter is only worth anything if SQL Server agrees with it.
/// </para>
/// <para>
/// The fixture creates and drops its own database per test class, so these tests do not touch the
/// development database.
/// </para>
/// </summary>
public class CompanyScopeTests : IAsyncLifetime
{
    private const int Tenant = 1;

    private EnsaTestFixture _fixture = null!;

    public Task InitializeAsync()
    {
        _fixture = new EnsaTestFixture(tenantId: Tenant, userId: 1, databaseCreate: true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    // ------------------------------------------------------------------ helpers

    /// <summary>Binds the user to a client workplace for the enclosing block, then releases them.</summary>
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

    private async Task<int> AddCompanyAsync(string name)
    {
        var company = new Company
        {
            CompanyName = name,
            HazardClass = HazardClass.Hazardous,
            WorkplaceType = WorkplaceType.Headquarter,
            CityId = 34,
            DistrictId = 1,
            IsActive = true
        };

        await using var context = _fixture.CreateContext();
        context.Set<Company>().Add(company);
        await context.SaveChangesAsync();
        return company.Id;
    }

    private async Task<int> AddEmployeeAsync(int companyId, string name)
    {
        var employee = new CompanyEmployee
        {
            CompanyId = companyId,
            Name = name,
            LastName = "Test",
            IsActive = true
        };

        await using var context = _fixture.CreateContext();
        context.Set<CompanyEmployee>().Add(employee);
        await context.SaveChangesAsync();
        return employee.Id;
    }

    // ------------------------------------------------------------------ tests

    [Fact]
    public async Task Staff_with_no_company_see_every_workplace()
    {
        var first = await AddCompanyAsync("First Ltd");
        var second = await AddCompanyAsync("Second Ltd");

        await using var context = _fixture.CreateContext();
        var visible = await context.Set<Company>().Select(c => c.Id).ToListAsync();

        Assert.Contains(first, visible);
        Assert.Contains(second, visible);
    }

    [Fact]
    public async Task A_company_user_sees_only_their_own_workplace()
    {
        var own = await AddCompanyAsync("Own Ltd");
        var other = await AddCompanyAsync("Other Ltd");

        using (AsCompanyUser(own))
        {
            await using var context = _fixture.CreateContext();

            Assert.Equal([own], await context.Set<Company>().Select(c => c.Id).ToListAsync());
            Assert.Null(await context.Set<Company>().FirstOrDefaultAsync(c => c.Id == other));
        }
    }

    [Fact]
    public async Task A_company_user_sees_only_their_own_workplaces_records()
    {
        var own = await AddCompanyAsync("Own Ltd");
        var other = await AddCompanyAsync("Other Ltd");

        var mine = await AddEmployeeAsync(own, "Mine");
        var theirs = await AddEmployeeAsync(other, "Theirs");

        using (AsCompanyUser(own))
        {
            await using var context = _fixture.CreateContext();
            var visible = await context.Set<CompanyEmployee>().Select(e => e.Id).ToListAsync();

            Assert.Equal([mine], visible);
            Assert.Null(await context.Set<CompanyEmployee>().FirstOrDefaultAsync(e => e.Id == theirs));
        }
    }

    [Fact]
    public async Task The_scope_fails_closed_on_a_row_that_belongs_to_no_workplace()
    {
        // A null CompanyId is provider-level data. Unlike a null TenantId, which marks shared
        // reference data, it is NOT shown to a user bound to a workplace.
        var own = await AddCompanyAsync("Own Ltd");

        int providerLevel;
        await using (var context = _fixture.CreateContext())
        {
            var document = new Ensa.Domain.Documents.Document
            {
                CompanyId = null,
                DocumentName = "provider-level.pdf",
                StorageName = Guid.NewGuid().ToString("N"),
                ContentType = "application/pdf",
                SizeBytes = 1,
                IsActive = true
            };

            context.Set<Ensa.Domain.Documents.Document>().Add(document);
            await context.SaveChangesAsync();
            providerLevel = document.Id;
        }

        using (AsCompanyUser(own))
        {
            await using var context = _fixture.CreateContext();

            Assert.Null(await context.Set<Ensa.Domain.Documents.Document>()
                .FirstOrDefaultAsync(d => d.Id == providerLevel));
        }
    }

    [Fact]
    public async Task Disabling_the_company_scope_reveals_every_workplace_and_restores_afterwards()
    {
        var own = await AddCompanyAsync("Own Ltd");
        var other = await AddCompanyAsync("Other Ltd");

        using (AsCompanyUser(own))
        {
            await using var context = _fixture.CreateContext();

            using (_fixture.DataFilter.Disable<ICompanyScoped>())
            {
                var visible = await context.Set<CompanyEmployee>().ToListAsync();
                Assert.Empty(visible); // no employees yet - the point is the Company set below
                Assert.Contains(other, await context.Set<Company>().Select(c => c.Id).ToListAsync());
            }

            Assert.Equal([own], await context.Set<Company>().Select(c => c.Id).ToListAsync());
        }
    }

    [Fact]
    public async Task Disabling_the_company_scope_does_not_disable_tenant_isolation()
    {
        var own = await AddCompanyAsync("Own Ltd");

        using (AsCompanyUser(own))
        {
            await using var context = _fixture.CreateContext();
            using (_fixture.DataFilter.Disable<ICompanyScoped>())
            {
                var visible = await context.Set<Company>().ToListAsync();
                Assert.All(visible, company => Assert.Equal(Tenant, company.TenantId));
            }
        }
    }
}
