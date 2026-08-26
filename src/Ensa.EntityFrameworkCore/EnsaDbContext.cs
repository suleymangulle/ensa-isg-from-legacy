using System.Linq.Expressions;
using System.Reflection;
using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.Ambient;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ensa.EntityFrameworkCore;

/// <summary>
/// The single <see cref="DbContext"/> of the Ensa application.
/// <para>
/// <b>DESIGN DECISION — no DbSets.</b> This class deliberately declares no
/// <c>DbSet&lt;T&gt;</c> at all. Entity discovery is done entirely through
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, Func{Type,bool}?)"/>.
/// The consequences:
/// <list type="bullet">
/// <item>Seven developers can work in parallel without a <b>merge conflict</b> in this file.</item>
/// <item>Writing a <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> for every
///       entity becomes <b>mandatory</b> (ARCHITECTURE.md §12). An entity without a configuration never
///       enters the model — a better failure mode than silently generating the wrong table.</item>
/// <item><see cref="NavigationEntity"/> derivatives (<c>[NotMapped]</c>) cannot slip into the model by accident.</item>
/// </list>
/// Access always goes through <c>Set&lt;TEntity&gt;()</c>, via the repository layer.
/// </para>
/// <para>
/// <b>The ambient services are optional.</b> In design-time (<c>dotnet ef</c>) and seed scenarios there may be
/// no DI container, so the constructor services may be <c>null</c> and safe defaults are used instead.
/// </para>
/// </summary>
public class EnsaDbContext : IdentityDbContext<User, Role, int>
{
    private static readonly MethodInfo ConfigureGlobalFiltersMethodInfo =
        typeof(EnsaDbContext).GetMethod(
            nameof(ConfigureGlobalFilters),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>Name of the scope column on an <see cref="ICompanyScoped"/> entity.</summary>
    private const string CompanyIdPropertyName = "CompanyId";

    /// <summary>Name of the scope column on an <see cref="ICompanyRecord"/> entity.</summary>
    private const string IdPropertyName = "Id";

    private readonly ICurrentTenant? _currentTenant;
    private readonly ICurrentUser? _currentUser;
    private readonly IClock? _clock;
    private readonly IDataFilter? _dataFilter;

    /// <param name="options">EF Core options.</param>
    /// <param name="currentTenant">Current tenant context. May be <c>null</c> at design time.</param>
    /// <param name="currentUser">Current user. May be <c>null</c> at design time.</param>
    /// <param name="clock">Time source. May be <c>null</c> at design time.</param>
    /// <param name="dataFilter">Global filter switch. May be <c>null</c> at design time.</param>
    public EnsaDbContext(
        DbContextOptions<EnsaDbContext> options,
        ICurrentTenant? currentTenant = null,
        ICurrentUser? currentUser = null,
        IClock? clock = null,
        IDataFilter? dataFilter = null)
        : base(options)
    {
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _clock = clock;
        _dataFilter = dataFilter;
    }

    // ------------------------------------------------------------------
    // Ambient context — the global filter expressions and audit fields use these
    // ------------------------------------------------------------------

    /// <summary>Current tenant id. <c>null</c> = host context.</summary>
    public virtual int? CurrentTenantId => _currentTenant?.Id;

    /// <summary>
    /// The client workplace the current user is bound to, or <c>null</c> when they are not bound to
    /// one - which is the case for the provider's own staff and for every call with no user at all
    /// (sign-in, seeding, background work). A <c>null</c> here means "no narrowing".
    /// </summary>
    public virtual int? CurrentCompanyId => _currentUser?.CompanyId;

    /// <summary>The "now" value to use in the audit fields.</summary>
    protected virtual DateTime Now => _clock?.Now ?? DateTime.Now;

    /// <summary>Id of the current user, to be used in the audit fields.</summary>
    protected virtual int? CurrentUserId => (_currentUser ?? NullCurrentUser.Instance).Id;

    /// <summary>
    /// Whether the soft-delete global filter is enabled.
    /// <para>
    /// This property is embedded <b>directly into the query filter expression</b>. EF Core lifts it out as a
    /// query parameter, so that calling <c>IDataFilter.Disable&lt;ISoftDelete&gt;()</c> disables the filter
    /// without recompiling the model (the ABP pattern).
    /// </para>
    /// </summary>
    public virtual bool IsSoftDeleteFilterEnabled => _dataFilter?.IsEnabled<ISoftDelete>() ?? true;

    /// <summary>
    /// Whether the multi-tenant global filter is enabled.
    /// <inheritdoc cref="IsSoftDeleteFilterEnabled" path="/summary/para" />
    /// </summary>
    public virtual bool IsMultiTenantFilterEnabled => _dataFilter?.IsEnabled<IMultiTenant>() ?? true;

    /// <summary>
    /// Whether the company-scope global filter is enabled.
    /// <inheritdoc cref="IsSoftDeleteFilterEnabled" path="/summary/para" />
    /// </summary>
    public virtual bool IsCompanyScopeFilterEnabled => _dataFilter?.IsEnabled<ICompanyScoped>() ?? true;

    /// <summary>
    /// Entities to be deleted <b>physically</b> in this <see cref="SaveChanges()"/> round.
    /// <para>
    /// An entity implementing <see cref="ISoftDelete"/> is normally converted to a logical delete by
    /// <see cref="ApplyEnsaConcepts"/>. Objects in this set are exempt from that conversion, i.e. they are
    /// really <c>DELETE</c>d. The set is cleared after every <c>SaveChanges</c>. It is populated by the
    /// repository's <c>HardDeleteAsync</c> method.
    /// </para>
    /// </summary>
    public HashSet<object> HardDeletedEntities { get; } = new(ReferenceEqualityComparer.Instance);

    // ------------------------------------------------------------------
    // Model building
    // ------------------------------------------------------------------

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // The Identity/OpenIddict tables are linked to the globally filtered User with a required foreign key.
        // That is expected here, so we stop EF from emitting a warning on every query.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1) All tables live in the "ensa" schema.
        builder.HasDefaultSchema(EnsaDomainSharedConsts.DbSchema);

        // 2) OpenIddict tables (int keys) — same key type as Identity.
        builder.UseOpenIddict<int>();

        // 3) Ensa naming for the Identity tables.
        ConfigureIdentityTables(builder);

        // 4) Entity discovery: every IEntityTypeConfiguration<T> class in the assembly.
        //    Runs after step 3 so that module-specific configurations can override it as well.
        builder.ApplyConfigurationsFromAssembly(typeof(EnsaDbContext).Assembly);

        // 5) Conventions — AFTER the configurations, because they must not overwrite
        //    values that were set explicitly.
        ConfigureDecimalPrecision(builder);
        DisableCascadeDelete(builder);
        ApplyGlobalFilters(builder);
    }

    /// <summary>Renames the ASP.NET Core Identity tables to the Ensa naming.</summary>
    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        const string schema = EnsaDomainSharedConsts.DbSchema;

        builder.Entity<User>().ToTable("User", schema);
        builder.Entity<Role>().ToTable("Role", schema);
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRole", schema);
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaim", schema);
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogin", schema);
        builder.Entity<IdentityUserToken<int>>().ToTable("UserToken", schema);
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaim", schema);
    }

    /// <summary>
    /// Gives every <c>decimal</c> / <c>decimal?</c> column the default <c>decimal(18,4)</c> precision.
    /// <para>
    /// Properties with an explicit <c>HasPrecision</c> / <c>HasColumnType</c> or a value converter are left
    /// alone; money fields can override this in their own configuration (e.g. <c>HasPrecision(18, 2)</c>).
    /// </para>
    /// </summary>
    private static void ConfigureDecimalPrecision(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType != typeof(decimal) && property.ClrType != typeof(decimal?))
                {
                    continue;
                }

                if (property.GetPrecision() is not null ||
                    property.GetColumnType() is not null ||
                    property.GetValueConverter() is not null)
                {
                    continue;
                }

                property.SetPrecision(18);
                property.SetScale(4);
            }
        }
    }

    /// <summary>
    /// Turns cascade delete off for every domain foreign key.
    /// <para>
    /// <b>Why:</b> in Ensa deleting data is almost always a <i>soft delete</i>, and a cascading physical
    /// delete silently destroys the audit trail. With <see cref="DeleteBehavior.Restrict"/>, attempting to
    /// delete a row that still has dependants fails; the business rule must handle that explicitly in the
    /// domain manager.
    /// </para>
    /// <para>
    /// <b>Exceptions:</b> (a) owned entity relationships — EF Core requires cascade for those; (b) the
    /// ASP.NET Core Identity and OpenIddict tables — framework APIs such as
    /// <c>UserManager.DeleteAsync</c> depend on those cascades.
    /// </para>
    /// </summary>
    private static void DisableCascadeDelete(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (IsFrameworkEntity(entityType.ClrType))
            {
                continue;
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                if (foreignKey.IsOwnership)
                {
                    continue;
                }

                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    /// <summary>Tells whether the type belongs to ASP.NET Core Identity or OpenIddict.</summary>
    private static bool IsFrameworkEntity(Type? clrType)
    {
        var ns = clrType?.Namespace;
        return ns is not null
               && (ns.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal)
                   || ns.StartsWith("OpenIddict", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------
    // Global query filters (soft delete + multi-tenant)
    // ------------------------------------------------------------------

    /// <summary>
    /// Configures a global query filter for every root entity type in the model, based on the interfaces it
    /// implements. The generic <see cref="ConfigureGlobalFilters{TEntity}"/> method is invoked per type via
    /// <see cref="MethodInfo.MakeGenericMethod"/>.
    /// </summary>
    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        // A snapshot is taken first because calling Entity<T>() can modify the model.
        foreach (var entityType in builder.Model.GetEntityTypes().ToList())
        {
            // In a TPH hierarchy the filter can only be applied to the root type.
            if (entityType.BaseType is not null || entityType.IsOwned())
            {
                continue;
            }

            var clrType = entityType.ClrType;
            if (!typeof(ISoftDelete).IsAssignableFrom(clrType)
                && !typeof(IMultiTenant).IsAssignableFrom(clrType)
                && !typeof(ICompanyScoped).IsAssignableFrom(clrType)
                && !typeof(ICompanyRecord).IsAssignableFrom(clrType))
            {
                continue;
            }

            ConfigureGlobalFiltersMethodInfo
                .MakeGenericMethod(clrType)
                .Invoke(this, [builder]);
        }
    }

    /// <summary>
    /// Builds and applies the filter expression for a single entity type.
    /// It is <c>private</c> but generic, because it is invoked through reflection.
    /// </summary>
    private void ConfigureGlobalFilters<TEntity>(ModelBuilder builder)
        where TEntity : class
    {
        var filter = CreateFilterExpression<TEntity>();
        if (filter is not null)
        {
            builder.Entity<TEntity>().HasQueryFilter(filter);
        }
    }

    /// <summary>
    /// Builds the combined filter expression for <typeparamref name="TEntity"/>.
    /// <para>
    /// The filters include the <c>IsXxxFilterEnabled</c> flag so that they can be switched off at runtime
    /// through <see cref="IDataFilter"/>:
    /// <c>e =&gt; !IsSoftDeleteFilterEnabled || !e.IsDeleted</c>
    /// </para>
    /// </summary>
    protected virtual Expression<Func<TEntity, bool>>? CreateFilterExpression<TEntity>()
        where TEntity : class
    {
        Expression<Func<TEntity, bool>>? expression = null;

        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            expression = e => !IsSoftDeleteFilterEnabled
                              || !EF.Property<bool>(e, nameof(ISoftDelete.IsDeleted));
        }

        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
        {
            // TenantId == null => host row; visible to every tenant.
            Expression<Func<TEntity, bool>> tenantFilter =
                e => !IsMultiTenantFilterEnabled
                     || EF.Property<int?>(e, nameof(IMultiTenant.TenantId)) == CurrentTenantId
                     || EF.Property<int?>(e, nameof(IMultiTenant.TenantId)) == null;

            expression = expression is null ? tenantFilter : CombineExpressions(expression, tenantFilter);
        }

        if (typeof(ICompanyScoped).IsAssignableFrom(typeof(TEntity)))
        {
            // CurrentCompanyId == null => the caller is not bound to a workplace, so nothing is
            // narrowed. A row whose CompanyId is null is provider-level data and is NOT shown to a
            // company-bound user: the scope fails closed, unlike the tenant filter, where a null
            // marks shared reference data.
            Expression<Func<TEntity, bool>> companyFilter =
                e => !IsCompanyScopeFilterEnabled
                     || CurrentCompanyId == null
                     || EF.Property<int?>(e, CompanyIdPropertyName) == CurrentCompanyId;

            expression = expression is null ? companyFilter : CombineExpressions(expression, companyFilter);
        }

        if (typeof(ICompanyRecord).IsAssignableFrom(typeof(TEntity)))
        {
            // The workplace itself: the scope key is the primary key.
            Expression<Func<TEntity, bool>> selfFilter =
                e => !IsCompanyScopeFilterEnabled
                     || CurrentCompanyId == null
                     || EF.Property<int>(e, IdPropertyName) == CurrentCompanyId;

            expression = expression is null ? selfFilter : CombineExpressions(expression, selfFilter);
        }

        return expression;
    }

    /// <summary><c>AND</c>s two predicates over a single parameter.</summary>
    private static Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "e");

        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    /// <summary>Replaces the parameter references in a lambda body with a shared parameter.</summary>
    private sealed class ReplaceParameterVisitor(Expression from, Expression to) : ExpressionVisitor
    {
        public override Expression? Visit(Expression? node) => node == from ? to : base.Visit(node);
    }

    // ------------------------------------------------------------------
    // SaveChanges — audit fields, tenant assignment, soft delete
    // ------------------------------------------------------------------

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyEnsaConcepts();
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        finally
        {
            HardDeletedEntities.Clear();
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyEnsaConcepts();
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        finally
        {
            HardDeletedEntities.Clear();
        }
    }

    /// <summary>
    /// Applies Ensa's cross-cutting rules right before saving: audit fields, tenant assignment and the
    /// soft delete conversion.
    /// <para>
    /// <b>Why an override rather than an interceptor?</b> A <c>SaveChangesInterceptor</c> would have worked
    /// too, but because the soft delete conversion <i>mutates</i> the <see cref="ChangeTracker"/> state,
    /// keeping the flow here — in one visible place — makes debugging easier.
    /// </para>
    /// </summary>
    protected virtual void ApplyEnsaConcepts()
    {
        ChangeTracker.DetectChanges();

        // We iterate over a snapshot of the collection because we are changing entity states.
        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyCreationConcepts(entry);
                    break;

                case EntityState.Modified:
                    ApplyModificationConcepts(entry);
                    break;

                case EntityState.Deleted:
                    ApplyDeletionConcepts(entry);
                    break;
            }
        }
    }

    /// <summary>Insert: creation audit + tenant assignment.</summary>
    protected virtual void ApplyCreationConcepts(EntityEntry entry)
    {
        // CreationTime is filled only when it has not been set; seed and data migration scenarios
        // must be able to supply a past date.
        if (entry.Entity is IHasCreationTime hasCreationTime && hasCreationTime.CreationTime == default)
        {
            hasCreationTime.CreationTime = Now;
        }

        if (entry.Entity is ICreationAudited creationAudited && creationAudited.CreatorId is null)
        {
            creationAudited.CreatorId = CurrentUserId;
        }

        // An explicitly assigned TenantId (e.g. host administration creating a row for another organization) is preserved.
        if (entry.Entity is IMultiTenant multiTenant && multiTenant.TenantId is null)
        {
            multiTenant.TenantId = CurrentTenantId;
        }
    }

    /// <summary>Update: modification audit.</summary>
    protected virtual void ApplyModificationConcepts(EntityEntry entry)
    {
        if (entry.Entity is IHasModificationTime hasModificationTime)
        {
            hasModificationTime.LastModificationTime = Now;
        }

        if (entry.Entity is IModificationAudited modificationAudited)
        {
            modificationAudited.LastModifierId = CurrentUserId;
        }
    }

    /// <summary>
    /// Delete: turns a physical delete into a logical one for entities implementing
    /// <see cref="ISoftDelete"/>.
    /// </summary>
    protected virtual void ApplyDeletionConcepts(EntityEntry entry)
    {
        // Leave it alone if Repository.HardDeleteAsync explicitly asked for a physical delete.
        if (HardDeletedEntities.Contains(entry.Entity))
        {
            return;
        }

        if (entry.Entity is not ISoftDelete softDelete)
        {
            return;
        }

        // UPDATE instead of a physical DELETE.
        entry.State = EntityState.Modified;

        softDelete.IsDeleted = true;

        if (entry.Entity is IDeletionAudited deletionAudited)
        {
            deletionAudited.DeletionTime = Now;
            deletionAudited.DeleterId = CurrentUserId;
        }

        // A delete is a modification too; the modification audit fields are updated as well.
        ApplyModificationConcepts(entry);
    }
}
