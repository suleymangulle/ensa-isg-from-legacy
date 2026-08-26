using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// Restriction of a permission per user type (allow/deny list). A tenant-owned record.
/// </summary>
public class PermissionRestrictionConfiguration : IEntityTypeConfiguration<PermissionRestriction>
{
    public void Configure(EntityTypeBuilder<PermissionRestriction> builder)
    {
        builder.ToTable("PermissionRestriction");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PermissionId);
        builder.HasIndex(x => x.UserTypeId);

        builder.HasIndex(x => new { x.TenantId, x.PermissionId, x.UserTypeId }).IsUnique();
    }
}
