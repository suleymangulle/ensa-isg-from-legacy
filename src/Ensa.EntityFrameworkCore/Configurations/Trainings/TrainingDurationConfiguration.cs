using Ensa.Domain.Trainings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="TrainingDuration"/> table mapping.
/// <para>
/// Uniqueness is on (<c>TrainingId</c>, <c>HazardClass</c>). <c>TenantId</c> is NOT added to the key:
/// <c>TrainingId</c> already points at a tenant-scoped record and therefore determines the tenant
/// transitively; adding it would only weaken the uniqueness.
/// </para>
/// </summary>
public class TrainingDurationConfiguration : IEntityTypeConfiguration<TrainingDuration>
{
    public void Configure(EntityTypeBuilder<TrainingDuration> builder)
    {
        builder.ToTable("TrainingDuration");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TrainingId, x.HazardClass })
               .IsUnique();
    }
}
