using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyActivity"/> table mapping.
/// </summary>
public class CompanyActivityConfiguration : IEntityTypeConfiguration<CompanyActivity>
{
    public void Configure(EntityTypeBuilder<CompanyActivity> builder)
    {
        builder.ToTable("CompanyActivity");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActivityCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // The company's activity list.
        builder.HasIndex(x => new { x.CompanyId, x.ActivityId });

        // Reverse lookup from an activity to its companies.
        builder.HasIndex(x => x.ActivityId);
    }
}
