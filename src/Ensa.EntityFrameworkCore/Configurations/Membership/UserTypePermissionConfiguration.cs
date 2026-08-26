using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// Default permission granted to a user type. A host definition; it carries no <c>TenantId</c>.
/// </summary>
public class UserTypePermissionConfiguration : IEntityTypeConfiguration<UserTypePermission>
{
    public void Configure(EntityTypeBuilder<UserTypePermission> builder)
    {
        builder.ToTable("UserTypePermission");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PermissionId);

        // No separate index for the UserTypeId foreign key: it is the LEADING column of this composite index.
        builder.HasIndex(x => new { x.UserTypeId, x.PermissionId }).IsUnique();
    }
}
