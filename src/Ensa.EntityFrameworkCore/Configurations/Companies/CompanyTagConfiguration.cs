using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyTag"/> table mapping (company-specific template placeholder).
/// </summary>
public class CompanyTagConfiguration : IEntityTypeConfiguration<CompanyTag>
{
    public void Configure(EntityTypeBuilder<CompanyTag> builder)
    {
        builder.ToTable("CompanyTag");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TagCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Tag)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // Template resolution goes through the code.
        // Not UNIQUE, because the legacy data may contain duplicate codes.
        builder.HasIndex(x => new { x.CompanyId, x.TagCode });
    }
}
