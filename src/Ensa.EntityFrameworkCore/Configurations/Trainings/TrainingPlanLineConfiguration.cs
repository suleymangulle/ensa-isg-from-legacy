using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="TrainingPlanLine"/> table mapping.
/// <para>
/// This is the most heavily queried table of the module: separate indexes are defined for the plan screen
/// (plan + year + month), for tracking training status per company and for the IBYS submission queue.
/// </para>
/// </summary>
public class TrainingPlanLineConfiguration : IEntityTypeConfiguration<TrainingPlanLine>
{
    public void Configure(EntityTypeBuilder<TrainingPlanLine> builder)
    {
        builder.ToTable("TrainingPlanLine");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Source)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.RejectionReason)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // External trainer national id — deterministic AES; equality and indexing still work.
        builder.Property(x => x.InstructorNationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.InstructorTitle)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.InstructorFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.IbysStatusCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.IbysMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // ---------------- Indexes ----------------

        // Plan screen: plan, year and month breakdown.
        builder.HasIndex(x => new { x.TrainingPlanId, x.Year, x.Month });

        // Tracking training status per company.
        builder.HasIndex(x => new { x.CompanyId, x.TrainingId, x.Status });

        // IBYS submission queue scan.
        builder.HasIndex(x => x.IbysStatus);

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.TrainingId);
        builder.HasIndex(x => x.InstructorUserId);
        builder.HasIndex(x => x.IbysQueryId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.PreviousLineId);
        builder.HasIndex(x => x.ForApprovalSenderUserId);
        builder.HasIndex(x => x.ApproverUserId);
    }
}
