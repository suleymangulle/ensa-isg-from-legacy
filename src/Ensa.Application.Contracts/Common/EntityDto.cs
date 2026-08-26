namespace Ensa.Application.Contracts.Common;

/// <summary>
/// Root class for every DTO.
/// RULE: DTOs must NOT declare navigation properties (properties of a class type).
/// When related data is needed, use a <see cref="NavigationDto"/> derivative instead.
/// </summary>
public abstract class EntityDto<TKey>
{
    public TKey Id { get; set; } = default!;
}

public abstract class EntityDto : EntityDto<int>;

public abstract class CreationAuditedEntityDto<TKey> : EntityDto<TKey>
{
    public DateTime CreationTime { get; set; }
    public int? CreatorId { get; set; }
}

public abstract class CreationAuditedEntityDto : CreationAuditedEntityDto<int>;

public abstract class AuditedEntityDto<TKey> : CreationAuditedEntityDto<TKey>
{
    public DateTime? LastModificationTime { get; set; }
    public int? LastModifierId { get; set; }
}

public abstract class AuditedEntityDto : AuditedEntityDto<int>;

public abstract class FullAuditedEntityDto<TKey> : AuditedEntityDto<TKey>
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
    public int? DeleterId { get; set; }
}

public abstract class FullAuditedEntityDto : FullAuditedEntityDto<int>;

/// <summary>A DTO that carries the tenant id (visible to host administrators only).</summary>
public interface IMultiTenantDto
{
    int? TenantId { get; set; }
}
