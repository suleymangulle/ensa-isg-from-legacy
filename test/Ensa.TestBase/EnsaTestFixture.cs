using Ensa.Domain.Common;
using Ensa.EntityFrameworkCore;
using Ensa.EntityFrameworkCore.Ambient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.TestBase;

/// <summary>
/// Builds an <see cref="EnsaDbContext"/> for tests.
/// <para>
/// <b>The provider is SQL Server and no connection is opened.</b> EF Core builds the model
/// lazily in memory, so <c>IEntityTypeConfiguration</c> errors, table clashes and mapping
/// problems surface the moment the model is touched. That is what lets the model validation
/// tests run without a live database.
/// </para>
/// <para>
/// SQLite cannot stand in as the provider: the configurations use SQL Server specific column
/// types such as <c>nvarchar(max)</c> and T-SQL filtered indexes via <c>HasFilter</c>.
/// Integration tests that work on real data must use LocalDB through
/// <see cref="DatabaseCreate"/>.
/// </para>
/// </summary>
public class EnsaTestFixture : IAsyncDisposable
{
    private readonly string _databaseName = $"EnsaTest_{Guid.NewGuid():N}";

    public EnsaTestFixture(
        int? tenantId = 1,
        int? userId = 1,
        bool databaseCreate = false,
        int? companyId = null)
    {
        DatabaseCreate = databaseCreate;

        var connectionString = databaseCreate
            ? $@"Server=(localdb)\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True"
            : @"Server=(localdb)\MSSQLLocalDB;Database=EnsaModelValidation;Trusted_Connection=True;TrustServerCertificate=True";

        Options = new DbContextOptionsBuilder<EnsaDbContext>()
            .UseSqlServer(connectionString)
            .EnableSensitiveDataLogging()
            .Options;

        TenantAccessor = new AsyncLocalCurrentTenantAccessor
        {
            Current = new TenantInfo(tenantId, "Test Organization")
        };

        Clock = new Clock();
        DataFilter = new DataFilter();
        CurrentUser = new TestCurrentUser(userId, tenantId, companyId);

        if (!databaseCreate)
        {
            return;
        }

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Whether a physical database was created (for integration tests).</summary>
    public bool DatabaseCreate { get; }

    public DbContextOptions<EnsaDbContext> Options { get; }

    public ICurrentTenantAccessor TenantAccessor { get; }

    public IClock Clock { get; }

    public IDataFilter DataFilter { get; }

    /// <summary>
    /// Typed deliberately: a test binds the user to a client workplace, or releases them from one,
    /// by assigning <see cref="TestCurrentUser.CompanyId"/> between contexts.
    /// </summary>
    public TestCurrentUser CurrentUser { get; }

    /// <summary>
    /// Creates a new <see cref="EnsaDbContext"/>.
    /// Using a separate context per test keeps the ChangeTracker cache from producing
    /// false positives.
    /// </summary>
    public EnsaDbContext CreateContext()
        => new(Options, new CurrentTenant(TenantAccessor), CurrentUser, Clock, DataFilter);

    public async ValueTask DisposeAsync()
    {
        if (DatabaseCreate)
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>Fixed user context used by the tests.</summary>
public sealed class TestCurrentUser(int? id, int? tenantId, int? companyId = null) : ICurrentUser
{
    public bool IsAuthenticated => id.HasValue;

    public int? Id => id;

    public string? UserName => id.HasValue ? "test-user" : null;

    public string? Email => id.HasValue ? "test@ensa.local" : null;

    public int? TenantId => tenantId;

    /// <summary>Set it to bind the test user to one client workplace and exercise the company scope.</summary>
    public int? CompanyId { get; set; } = companyId;

    public string[] Roles { get; set; } = [];

    public string[] Permissions { get; set; } = [];

    public bool IsInRole(string roleName) => Roles.Contains(roleName);

    public bool HasPermission(string permissionName) => Permissions.Contains(permissionName);
}
