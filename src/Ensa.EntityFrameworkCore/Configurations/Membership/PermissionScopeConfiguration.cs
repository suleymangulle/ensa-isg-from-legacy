using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// Link between a permission and an object such as a module, a user type or a menu. A host definition.
/// <para>
/// The target is polymorphic (<c>LinkType</c> + id OR code), so no real foreign key can be configured; the
/// lookup indexes are composite with the target type.
/// </para>
/// </summary>
public class PermissionScopeConfiguration : IEntityTypeConfiguration<PermissionScope>
{
    public void Configure(EntityTypeBuilder<PermissionScope> builder)
    {
        builder.ToTable("PermissionScope");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LinkTargetCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.PermissionId);
        builder.HasIndex(x => new { x.LinkType, x.LinkTargetId });
        builder.HasIndex(x => new { x.LinkType, x.LinkTargetCode });
    }
}
