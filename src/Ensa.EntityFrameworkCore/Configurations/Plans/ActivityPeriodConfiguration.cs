using Ensa.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Plans;

/// <summary>
/// <see cref="ActivityPeriod"/> (link table between an activity and a period) mapping.
/// </summary>
public class ActivityPeriodConfiguration : IEntityTypeConfiguration<ActivityPeriod>
{
    public void Configure(EntityTypeBuilder<ActivityPeriod> builder)
    {
        builder.ToTable("ActivityPeriod");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ActivityId, x.PeriodId })
               .IsUnique();

        builder.HasIndex(x => x.PeriodId);
    }
}
