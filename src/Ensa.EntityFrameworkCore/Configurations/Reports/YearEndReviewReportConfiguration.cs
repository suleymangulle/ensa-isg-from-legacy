using Ensa.Domain.Reports;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Reports;

/// <summary><see cref="YearEndReviewReport"/> table mapping.</summary>
public class YearEndReviewReportConfiguration : IEntityTypeConfiguration<YearEndReviewReport>
{
    public void Configure(EntityTypeBuilder<YearEndReviewReport> builder)
    {
        builder.ToTable("YearEndReviewReport");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportTitle)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // Free-text names that must remain in the report even if the user is deleted.
        builder.Property(x => x.SpecialistFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.PhysicianFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.DeputyFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---------------- Indexes ----------------

        builder.HasIndex(x => new { x.TenantId, x.CompanyId });

        // Report search by year.
        builder.HasIndex(x => new { x.TenantId, x.ReportDate });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.SpecialistUserId);
        builder.HasIndex(x => x.PhysicianUserId);
    }
}
