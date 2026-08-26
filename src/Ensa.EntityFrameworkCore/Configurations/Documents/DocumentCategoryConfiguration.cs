using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="DocumentCategory"/> table mapping.
/// </summary>
public class DocumentCategoryConfiguration : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> builder)
    {
        builder.ToTable("DocumentCategory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CategoryCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.CategoryName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Category codes are unique within a tenant.
        builder.HasIndex(x => new { x.TenantId, x.CategoryCode }).IsUnique();

        builder.HasIndex(x => x.ReportingArticleGroup);
    }
}
