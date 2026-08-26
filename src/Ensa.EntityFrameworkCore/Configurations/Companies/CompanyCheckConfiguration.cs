using Ensa.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyCheck"/> table mapping (monthly checklist header).
/// </summary>
public class CompanyCheckConfiguration : IEntityTypeConfiguration<CompanyCheck>
{
    public void Configure(EntityTypeBuilder<CompanyCheck> builder)
    {
        builder.ToTable("CompanyCheck");
        builder.HasKey(x => x.Id);

        // A company can have only one check header per month.
        builder.HasIndex(x => new { x.CompanyId, x.CheckMonth })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.DocumentId);
    }
}
