using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="Parameter"/> table mapping (tenant-owned key/value setting).
/// </summary>
public class ParameterConfiguration : IEntityTypeConfiguration<Parameter>
{
    public void Configure(EntityTypeBuilder<Parameter> builder)
    {
        builder.ToTable("Parameter");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Value)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // Parameter codes are unique within a tenant.
        // Rows with a null TenantId (host) act as the default value.
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}
