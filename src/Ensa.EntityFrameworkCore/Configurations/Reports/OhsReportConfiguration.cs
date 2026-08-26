using Ensa.Domain.Reports;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Reports;

/// <summary>
/// <see cref="OhsReport"/> table mapping.
/// <para>
/// <see cref="OhsReport.NationalId"/> is encrypted with deterministic AES; because the converter is
/// deterministic, the index and the equality queries on that column keep working.
/// </para>
/// </summary>
public class OhsReportConfiguration : IEntityTypeConfiguration<OhsReport>
{
    public void Configure(EntityTypeBuilder<OhsReport> builder)
    {
        builder.ToTable("OhsReport");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NationalId)
               .IsRequired()
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.EmployeeName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---------------- Indexes ----------------

        // Assignment reports per office.
        builder.HasIndex(x => new { x.TenantId, x.OfficeId });

        // The employee's assignment history.
        builder.HasIndex(x => x.NationalId);

        // Foreign key index (no relationship is configured — index only).
        builder.HasIndex(x => x.ModuleArchiveDetailId);
    }
}
