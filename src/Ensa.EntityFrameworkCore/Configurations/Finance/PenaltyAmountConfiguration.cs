using Ensa.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary>
/// <see cref="PenaltyAmount"/> table mapping.
/// <para>A HOST reference table — there is no <c>TenantId</c> index.</para>
/// <para>
/// Uniqueness is on (<c>PenaltyId</c>, <c>HazardClass</c>, <c>EmployeeCountRange</c>,
/// <c>ValidityYear</c>): a penalty article can have only one amount for a given year, hazard class and
/// employee count range.
/// </para>
/// </summary>
public class PenaltyAmountConfiguration : IEntityTypeConfiguration<PenaltyAmount>
{
    public void Configure(EntityTypeBuilder<PenaltyAmount> builder)
    {
        builder.ToTable("PenaltyAmount");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new
               {
                   x.PenaltyId,
                   x.HazardClass,
                   x.EmployeeCountRange,
                   x.ValidityYear
               })
               .IsUnique();

        // Fetching every penalty amount for the current year in one go.
        builder.HasIndex(x => x.ValidityYear);
    }
}
