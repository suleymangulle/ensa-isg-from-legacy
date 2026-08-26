using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>
/// Hazard line identified in a risk assessment report.
/// <para>
/// The score fields are <c>decimal(9,2)</c>: this carries both the L-matrix (1-5) and the Fine-Kinney values
/// (multipliers between 0.5 and 100, score capped at 10,000) without loss.
/// </para>
/// </summary>
public class IdentifiedHazardConfiguration : IEntityTypeConfiguration<IdentifiedHazard>
{
    public void Configure(EntityTypeBuilder<IdentifiedHazard> builder)
    {
        builder.ToTable("IdentifiedHazard");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HazardTag)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.ActivityDescription)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.OwnerPerson)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.RiskTag)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Measure)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Comment)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.ResidualComment)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.Likelihood).HasPrecision(9, 2);
        builder.Property(x => x.Severity).HasPrecision(9, 2);
        builder.Property(x => x.Frequency).HasPrecision(9, 2);
        builder.Property(x => x.RiskScore).HasPrecision(9, 2);
        builder.Property(x => x.ResidualLikelihood).HasPrecision(9, 2);
        builder.Property(x => x.ResidualSeverity).HasPrecision(9, 2);
        builder.Property(x => x.ResidualFrequency).HasPrecision(9, 2);
        builder.Property(x => x.ResidualRiskScore).HasPrecision(9, 2);

        builder.HasIndex(x => x.RiskAssessmentReportId);
        builder.HasIndex(x => new { x.TenantId, x.RiskScore });

        // Source tracing: used to find the hazards generated from a field observation line or a corrective action.
        builder.HasIndex(x => new { x.SourceType, x.SourceId });

        // Tracking hazards past their due date.
        builder.HasIndex(x => x.DeadlineDate)
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.HazardCategoryId);
        builder.HasIndex(x => x.HazardId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
