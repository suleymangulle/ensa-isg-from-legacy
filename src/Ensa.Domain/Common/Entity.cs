namespace Ensa.Domain.Common;

/// <summary>
/// Root of the entity hierarchy.
/// <para>
/// RULE: no type in this hierarchy may declare a navigation property (a class-typed property).
/// Relationships are represented purely by foreign-key ids; when combined data is needed, use a
/// <see cref="NavigationEntity"/> derivative instead.
/// </para>
/// </summary>
public abstract class Entity<TKey> : IEntity<TKey>
{
    public virtual TKey Id { get; set; } = default!;

    public virtual object?[] GetKeys() => [Id];

    public override string ToString() => $"[{GetType().Name}] Id = {Id}";

    public override bool Equals(object? obj)
        => obj is Entity<TKey> other
           && other.GetType() == GetType()
           && EqualityComparer<TKey>.Default.Equals(Id, other.Id)
           && !EqualityComparer<TKey>.Default.Equals(Id, default);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>An entity that records who created it and when.</summary>
public abstract class CreationAuditedEntity<TKey> : Entity<TKey>, ICreationAudited
{
    public virtual DateTime CreationTime { get; set; }
    public virtual int? CreatorId { get; set; }
}

/// <summary>An entity that records creation and last modification.</summary>
public abstract class AuditedEntity<TKey> : CreationAuditedEntity<TKey>, IAudited
{
    public virtual DateTime? LastModificationTime { get; set; }
    public virtual int? LastModifierId { get; set; }
}

/// <summary>An entity that records creation, modification and soft deletion.</summary>
public abstract class FullAuditedEntity<TKey> : AuditedEntity<TKey>, IFullAudited
{
    public virtual bool IsDeleted { get; set; }
    public virtual DateTime? DeletionTime { get; set; }
    public virtual int? DeleterId { get; set; }
}

/// <summary>Tenant-owned, fully audited entity — the workhorse base class of this domain.</summary>
public abstract class FullAuditedTenantEntity<TKey> : FullAuditedEntity<TKey>, IMultiTenant
{
    public virtual int? TenantId { get; set; }
}

/// <summary>Tenant-owned entity that is updated but never soft-deleted.</summary>
public abstract class AuditedTenantEntity<TKey> : AuditedEntity<TKey>, IMultiTenant
{
    public virtual int? TenantId { get; set; }
}

/// <summary>Tenant-owned append-only entity — ledger rows, logs, audit trails.</summary>
public abstract class CreationAuditedTenantEntity<TKey> : CreationAuditedEntity<TKey>, IMultiTenant
{
    public virtual int? TenantId { get; set; }
}

// ---- int-keyed shorthands (almost every entity here uses an int key) ----

public abstract class Entity : Entity<int>;

public abstract class CreationAuditedEntity : CreationAuditedEntity<int>;

public abstract class AuditedEntity : AuditedEntity<int>;

public abstract class FullAuditedEntity : FullAuditedEntity<int>;

public abstract class FullAuditedTenantEntity : FullAuditedTenantEntity<int>;

public abstract class AuditedTenantEntity : AuditedTenantEntity<int>;

public abstract class CreationAuditedTenantEntity : CreationAuditedTenantEntity<int>;
