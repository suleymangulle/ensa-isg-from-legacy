using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary><see cref="TrainingPlan"/> table mapping.</summary>
public class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> builder)
    {
        builder.ToTable("TrainingPlan");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RevisionNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.DocumentNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- Indexes ----------------

        // The company's plan history (newest plan first).
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.StartDate });

        // Export queue scan for the external system.
        builder.HasIndex(x => new { x.TenantId, x.Transferred });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.SpecialistUserId);
        builder.HasIndex(x => x.PhysicianUserId);
        builder.HasIndex(x => x.ApproverUserId);
    }
}
