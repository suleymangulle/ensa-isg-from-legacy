using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// A permission enabled for an organization type — a mandatory gate in the permission calculation. A host definition.
/// </summary>
public class OrganizationTypePermissionConfiguration : IEntityTypeConfiguration<OrganizationTypePermission>
{
    public void Configure(EntityTypeBuilder<OrganizationTypePermission> builder)
    {
        builder.ToTable("OrganizationTypePermission");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PermissionId);

        // No separate index for the OrganizationTypeId foreign key: it is the LEADING column of this composite index.
        builder.HasIndex(x => new { x.OrganizationTypeId, x.PermissionId }).IsUnique();
    }
}
