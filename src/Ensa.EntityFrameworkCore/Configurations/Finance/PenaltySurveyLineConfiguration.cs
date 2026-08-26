using Ensa.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="PenaltySurveyLine"/> table mapping.</summary>
public class PenaltySurveyLineConfiguration : IEntityTypeConfiguration<PenaltySurveyLine>
{
    public void Configure(EntityTypeBuilder<PenaltySurveyLine> builder)
    {
        builder.ToTable("PenaltySurveyLine");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PenaltyAmount).HasPrecision(18, 2);

        // A multiplier is not money; four decimals because it is a ratio.
        builder.Property(x => x.Multiplier).HasPrecision(18, 4);

        builder.HasIndex(x => x.PenaltySurveyId);
        builder.HasIndex(x => x.PenaltyId);
    }
}
