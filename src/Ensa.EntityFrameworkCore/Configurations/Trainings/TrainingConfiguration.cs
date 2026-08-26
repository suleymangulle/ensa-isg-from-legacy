using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="Training"/> table mapping.
/// <para>
/// This is a MIXED (host + tenant) library table: rows with <c>TenantId = null</c> are the shared template
/// trainings available to every organization. Uniqueness is therefore composite with <c>TenantId</c>.
/// </para>
/// </summary>
public class TrainingConfiguration : IEntityTypeConfiguration<Training>
{
    public void Configure(EntityTypeBuilder<Training> builder)
    {
        builder.ToTable("Training");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TrainingName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.TrainingCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- Indexes ----------------

        // Training codes are unique within a tenant; host templates are unique among themselves.
        // Empty codes and deleted rows are out of scope.
        builder.HasIndex(x => new { x.TenantId, x.TrainingCode })
               .IsUnique()
               .HasFilter("[TrainingCode] IS NOT NULL AND [IsDeleted] = 0");

        // Catalogue listing: active trainings.
        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        // Scanning the default trainings during plan generation.
        builder.HasIndex(x => new { x.TenantId, x.IncludedInDefaultPlan });

        // Foreign key index (no relationship is configured — index only).
        builder.HasIndex(x => x.TrainingGroupId);
    }
}
