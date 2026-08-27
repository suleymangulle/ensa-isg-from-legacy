using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// <see cref="Role"/> configuration. The table name is set by <c>EnsaDbContext.ConfigureIdentityTables</c>,
/// so it is not repeated here.
/// <para>
/// Identity's GLOBAL unique index on <c>NormalizedName</c> (<c>RoleNameIndex</c>) is wrong in a multi-tenant
/// model; it is removed and replaced with a composite unique index on <c>(TenantId, NormalizedName)</c>.
/// Rows with <c>TenantId == null</c> are host (system) roles.
/// </para>
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(x => x.TenantId);

        IdentityIndexHelper.RemoveIndexOn(builder, nameof(Role.NormalizedName));

        builder.HasIndex(x => new { x.TenantId, x.NormalizedName })
               .IsUnique()
               .HasDatabaseName("IX_Role_TenantId_NormalizedName");
    }
}
