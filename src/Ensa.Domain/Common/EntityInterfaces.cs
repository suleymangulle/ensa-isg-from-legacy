namespace Ensa.Domain.Common;

/// <summary>Marker for every entity in the domain.</summary>
public interface IEntity
{
    object?[] GetKeys();
}

/// <summary>An entity with a single primary key.</summary>
public interface IEntity<TKey> : IEntity
{
    TKey Id { get; set; }
}

/// <summary>
/// Tenant discriminator. <c>null</c> means a host record — one shared by every tenant.
/// </summary>
public interface IMultiTenant
{
    int? TenantId { get; set; }
}

/// <summary>
/// The entity belongs to a single client workplace and carries a <c>CompanyId</c> property.
/// <para>
/// A user who is bound to a company (a customer contact) sees only the rows of that company; for
/// everybody else - the provider's own staff, who are bound to no company - the filter is inert.
/// The property is reached by name through <c>EF.Property</c>, so both <c>int</c> and <c>int?</c>
/// declarations work; a null <c>CompanyId</c> is provider-level data and stays hidden from a
/// company-bound user.
/// </para>
/// </summary>
public interface ICompanyScoped
{
}

/// <summary>
/// The entity <b>is</b> the client workplace, so its scope key is its own <c>Id</c>.
/// <para>
/// Only <c>Company</c> implements this. It is a separate marker because the filter has to compare
/// a different column, not because the rule differs.
/// </para>
/// </summary>
public interface ICompanyRecord
{
}

public interface IHasCreationTime
{
    DateTime CreationTime { get; set; }
}

public interface ICreationAudited : IHasCreationTime
{
    int? CreatorId { get; set; }
}

public interface IHasModificationTime
{
    DateTime? LastModificationTime { get; set; }
}

public interface IModificationAudited : IHasModificationTime
{
    int? LastModifierId { get; set; }
}

public interface IAudited : ICreationAudited, IModificationAudited;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface IDeletionAudited : ISoftDelete
{
    DateTime? DeletionTime { get; set; }
    int? DeleterId { get; set; }
}

public interface IFullAudited : IAudited, IDeletionAudited;

/// <summary>Records that can be switched on and off without being deleted.</summary>
public interface IActivatable
{
    bool IsActive { get; set; }
}

/// <summary>Records with an explicit display order — typically lookup tables.</summary>
public interface IHasSortOrder
{
    int SortOrder { get; set; }
}
