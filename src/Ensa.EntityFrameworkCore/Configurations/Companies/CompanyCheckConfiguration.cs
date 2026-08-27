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

        // A company's checks for a month. NOT unique: the legacy system opens a second header
        // rather than reopening the first when a month is checked again, and 12 of the 206
        // migrated headers are repeats -- several with a different status, so they are separate
        // rounds rather than duplicates. The 119 check lines under those headers name the header
        // they belong to, so merging the headers would move a company's answers onto a round they
        // were not given in.
        builder.HasIndex(x => new { x.CompanyId, x.CheckMonth })
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.DocumentId);
    }
}
