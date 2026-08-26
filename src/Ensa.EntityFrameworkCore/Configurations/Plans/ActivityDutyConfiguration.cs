using Ensa.Domain.Plans;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Plans;

/// <summary>
/// <see cref="ActivityDuty"/> (link table between an activity and a duty) mapping.
/// <para>
/// <see cref="ActivityDuty.DutyCode"/> is a legacy free-text field; it is carried alongside
/// <see cref="ActivityDuty.DutyId"/> until normalisation is complete. Because of that transition there is
/// no UNIQUE index on (ActivityId, DutyId).
/// </para>
/// </summary>
public class ActivityDutyConfiguration : IEntityTypeConfiguration<ActivityDuty>
{
    public void Configure(EntityTypeBuilder<ActivityDuty> builder)
    {
        builder.ToTable("ActivityDuty");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DutyCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.ActivityId);
        builder.HasIndex(x => x.DutyId);
    }
}
