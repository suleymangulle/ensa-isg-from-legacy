using System.Reflection;
using Ensa.Domain.Common;
using Ensa.EntityFrameworkCore;
using Ensa.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Tests;

/// <summary>
/// Verifies that the EF Core model honours the architecture contract.
/// <para>
/// These tests inspect the <b>whole model</b> rather than individual entities, so a rule
/// violation introduced by a new entity is caught automatically.
/// </para>
/// </summary>
public class ModelValidationTests : IAsyncLifetime
{
    private EnsaTestFixture _fixture = null!;
    private EnsaDbContext _context = null!;

    public Task InitializeAsync()
    {
        _fixture = new EnsaTestFixture();
        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public void Model_builds_without_errors()
    {
        // The model is built lazily, so touching it is enough;
        // a broken IEntityTypeConfiguration throws right here.
        var entityTypes = _context.Model.GetEntityTypes().ToList();

        Assert.NotEmpty(entityTypes);
    }

    [Fact]
    public void Navigation_entities_stay_out_of_the_model()
    {
        var navigationEntities = _context.Model.GetEntityTypes()
            .Where(e => typeof(NavigationEntity).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.True(
            navigationEntities.Count == 0,
            $"Navigation entities must stay out of the DbSets and the model: {string.Join(", ", navigationEntities)}");
    }

    [Fact]
    public void No_entity_has_a_navigation_property()
    {
        var violations = new List<string>();

        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            foreach (var navigation in entityType.GetNavigations())
            {
                violations.Add($"{entityType.ClrType.Name}.{navigation.Name}");
            }

            foreach (var skipNavigation in entityType.GetSkipNavigations())
            {
                violations.Add($"{entityType.ClrType.Name}.{skipNavigation.Name} (skip)");
            }
        }

        // The Identity and OpenIddict tables are exempt from this rule.
        var ours = violations
            .Where(i => !i.StartsWith("Identity", StringComparison.Ordinal)
                        && !i.StartsWith("OpenIddict", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            ours.Count == 0,
            $"Entities must not declare navigation properties. Violations: {string.Join(", ", ours)}");
    }

    [Fact]
    public void All_string_columns_have_a_length()
    {
        var unbounded = new List<string>();

        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            if (EnsaOutside(entityType.ClrType))
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType != typeof(string))
                {
                    continue;
                }

                // Where nvarchar(max) is a deliberate choice, the column type is written out explicitly.
                var columnType = property.GetColumnType();
                if (!string.IsNullOrEmpty(columnType))
                {
                    continue;
                }

                if (property.GetMaxLength() is null)
                {
                    unbounded.Add($"{entityType.ClrType.Name}.{property.Name}");
                }
            }
        }

        Assert.True(
            unbounded.Count == 0,
            "Every string column needs HasMaxLength or an explicit column type. " +
            $"Missing ({unbounded.Count}): {string.Join(", ", unbounded.Take(40))}");
    }

    [Fact]
    public void Tenant_scoped_entities_have_a_global_query_filter()
    {
        var unfiltered = _context.Model.GetEntityTypes()
            .Where(e => typeof(IMultiTenant).IsAssignableFrom(e.ClrType)
                        || typeof(ISoftDelete).IsAssignableFrom(e.ClrType))
            .Where(e => e.GetDeclaredQueryFilters().Count == 0)
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.True(
            unfiltered.Count == 0,
            "Every entity that is tenant-scoped or soft-deletable must get a global query filter. " +
            $"Missing: {string.Join(", ", unfiltered)}");
    }

    [Fact]
    public void Every_entity_with_a_company_lives_inside_the_company_scope()
    {
        // The scope is a global query filter, so it can only protect what is marked. An entity
        // that carries a CompanyId and forgets the marker is exactly the leak the filter exists to
        // close - and it leaks silently, with no error anywhere. If a new entity genuinely holds
        // provider-level data under a column named CompanyId, exempt it here on purpose.
        // Office is exempt, on purpose and for exactly the reason the paragraph above allows for.
        // An office belongs to the organization; Office.CompanyId is an attribution carried over
        // from the legacy COFirmaId column and is null on every one of the 957 migrated rows. Marking
        // the entity company-scoped therefore hid every office from every company-bound user —
        // the filter fails closed on a null scope key — including the offices such a user was
        // assigned to. It is listed here rather than silently skipped so the exemption stays a
        // decision somebody made.
        var providerLevelCompanyColumns = new[] { nameof(Ensa.Domain.Tenancy.Office) };

        var unmarked = _context.Model.GetEntityTypes()
            .Where(e => e.FindProperty("CompanyId") is not null)
            .Where(e => !typeof(ICompanyScoped).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.Name)
            .Where(name => !providerLevelCompanyColumns.Contains(name))
            .ToList();

        Assert.True(
            unmarked.Count == 0,
            "Every entity with a CompanyId must implement ICompanyScoped, or a user bound to one "
            + $"workplace reads another workplace's rows. Missing: {string.Join(", ", unmarked)}");
    }

    [Fact]
    public void The_company_record_itself_is_scoped_by_its_own_key()
    {
        // Company has no CompanyId - its scope key is its own Id, which is what ICompanyRecord
        // means. Without it a customer would be filtered out of every table except the one listing
        // the workplaces themselves.
        Assert.True(typeof(ICompanyRecord).IsAssignableFrom(typeof(Ensa.Domain.Companies.Company)));

        var filters = _context.Model.FindEntityType(typeof(Ensa.Domain.Companies.Company))!
            .GetDeclaredQueryFilters();

        Assert.NotEmpty(filters);
    }

    [Fact]
    public void All_domain_entities_are_configured()
    {
        var domainAssembly = typeof(Ensa.Domain.Companies.Company).Assembly;

        var expected = domainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IEntity).IsAssignableFrom(t))
            .Where(t => !typeof(NavigationEntity).IsAssignableFrom(t))
            .ToList();

        var inTheModel = _context.Model.GetEntityTypes()
            .Select(e => e.ClrType)
            .ToHashSet();

        var missing = expected
            .Where(t => !inTheModel.Contains(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "An entity without a configuration never reaches the database. " +
            $"Missing ({missing.Count}): {string.Join(", ", missing)}");
    }

    [Fact]
    public void Money_columns_declare_their_precision()
    {
        var withoutPrecision = new List<string>();

        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            if (EnsaOutside(entityType.ClrType))
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type != typeof(decimal))
                {
                    continue;
                }

                if (property.GetPrecision() is null && string.IsNullOrEmpty(property.GetColumnType()))
                {
                    withoutPrecision.Add($"{entityType.ClrType.Name}.{property.Name}");
                }
            }
        }

        Assert.True(
            withoutPrecision.Count == 0,
            "decimal columns must declare HasPrecision (the DbContext global default counts). " +
            $"Missing: {string.Join(", ", withoutPrecision.Take(40))}");
    }

    /// <summary>The Identity and OpenIddict tables are not subject to our rules.</summary>
    private static bool EnsaOutside(Type clrType)
        => clrType.Namespace is not null
           && !clrType.Namespace.StartsWith("Ensa.", StringComparison.Ordinal);
}
