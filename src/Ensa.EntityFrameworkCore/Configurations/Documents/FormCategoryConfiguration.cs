using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="FormCategory"/> table mapping.
/// </summary>
public class FormCategoryConfiguration : IEntityTypeConfiguration<FormCategory>
{
    public void Configure(EntityTypeBuilder<FormCategory> builder)
    {
        builder.ToTable("FormCategory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CategoryName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Category names are unique within a tenant.
        builder.HasIndex(x => new { x.TenantId, x.CategoryName }).IsUnique();
    }
}
