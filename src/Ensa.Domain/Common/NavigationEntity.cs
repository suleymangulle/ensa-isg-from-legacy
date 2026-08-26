using System.ComponentModel.DataAnnotations.Schema;

namespace Ensa.Domain.Common;

/// <summary>
/// ABP-style "navigation entity".
/// <para>
/// Entities may not declare navigation properties, so whenever several entities must be presented
/// together, a derivative of this type carries the combination.
/// </para>
/// <para>
/// Rules:
/// <list type="bullet">
/// <item>Marked <see cref="NotMappedAttribute"/>.</item>
/// <item>Never declared as a <c>DbSet</c>; never reaches <c>ModelBuilder</c>.</item>
/// <item>Has no table; it is filled by explicit projection in the repository layer.</item>
/// <item>Named <c>{Entity}Navigation</c>, e.g. <c>CompanyNavigation</c>.</item>
/// </list>
/// </para>
/// </summary>
[NotMapped]
public abstract class NavigationEntity
{
}

/// <summary>A navigation entity that wraps a single root entity.</summary>
[NotMapped]
public abstract class NavigationEntity<TEntity> : NavigationEntity
    where TEntity : class, IEntity
{
    /// <summary>The mapped root entity.</summary>
    public TEntity Entity { get; set; } = default!;
}
