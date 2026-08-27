using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

public class PermissionEndpointConfiguration : IEntityTypeConfiguration<PermissionEndpoint>
{
    public void Configure(EntityTypeBuilder<PermissionEndpoint> builder)
    {
        builder.ToTable("PermissionEndpoint");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ControllerName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.ActionName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // One row per endpoint. A second row for the same controller and action would make the
        // lookup ambiguous, and an ambiguous authorization answer is worse than a wrong one.
        builder.HasIndex(x => new { x.ControllerName, x.ActionName }).IsUnique();

        builder.HasIndex(x => x.PermissionId);
    }
}
