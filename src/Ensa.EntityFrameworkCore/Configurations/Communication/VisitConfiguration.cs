using Ensa.Domain.Communication;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Communication;

/// <summary><see cref="Visit"/> table mapping.</summary>
public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visit");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Color)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Color);

        // Distance is not money; two decimals in kilometres are enough for route planning.
        builder.Property(x => x.OtherCompanyDistanceKm).HasPrecision(9, 2);

        // ---------------- Indexes ----------------

        // Calendar view: the user's records within a date range.
        builder.HasIndex(x => new { x.UserId, x.Start, x.End });

        // Company visit history.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.VisitDate });
    }
}
