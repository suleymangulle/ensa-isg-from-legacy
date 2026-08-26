using Ensa.Domain.Plans;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Plans;

/// <summary>
/// <see cref="WorkPlanLine"/> table mapping.
/// <para>
/// Separate indexes are defined for the plan screen (plan + year + month), the approval workflow and
/// per-company tracking.
/// </para>
/// </summary>
public class WorkPlanLineConfiguration : IEntityTypeConfiguration<WorkPlanLine>
{
    public void Configure(EntityTypeBuilder<WorkPlanLine> builder)
    {
        builder.ToTable("WorkPlanLine");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.RejectionReason)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // External trainer national id — deterministic AES; equality and indexing still work.
        builder.Property(x => x.InstructorNationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        // ---------------- Indexes ----------------

        // Plan screen: plan, year and month breakdown.
        builder.HasIndex(x => new { x.WorkPlanId, x.Year, x.Month });

        // Activity-based approval workflow lists.
        builder.HasIndex(x => new { x.ActivityId, x.ApprovalStatus });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.PeriodId);
        builder.HasIndex(x => x.PreviousLineId);
        builder.HasIndex(x => x.ForApprovalSenderUserId);
        builder.HasIndex(x => x.ApproverUserId);
        builder.HasIndex(x => x.InstructorUserId);
    }
}
