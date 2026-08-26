using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// User-specific permission grant or denial. A tenant-owned record;
/// a user–permission pair can be defined only once per organization.
/// </summary>
public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermission");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.PermissionId);

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.PermissionId }).IsUnique();
    }
}
