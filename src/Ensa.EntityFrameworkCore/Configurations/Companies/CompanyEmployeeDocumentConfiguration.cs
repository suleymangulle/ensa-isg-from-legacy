using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyEmployeeDocument"/> table mapping (employee document + IBYS submission status).
/// </summary>
public class CompanyEmployeeDocumentConfiguration : IEntityTypeConfiguration<CompanyEmployeeDocument>
{
    public void Configure(EntityTypeBuilder<CompanyEmployeeDocument> builder)
    {
        builder.ToTable("CompanyEmployeeDocument");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OtherCertificateName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.GroupCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Source)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.IbysStatusCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.IbysMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.IbysNotificationNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- Indexes ----------------

        // "The employee's most recent document for this training" query.
        builder.HasIndex(x => new { x.CompanyEmployeeId, x.TrainingId, x.DocumentDate });

        // IBYS queue: rows awaiting submission or failed.
        builder.HasIndex(x => x.IbysStatus);

        // Rollback and reporting by bulk operation batch.
        builder.HasIndex(x => x.GroupCode);

        // Foreign key indexes.
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.TrainingId);
        builder.HasIndex(x => x.TrainingPlanLineId);
        builder.HasIndex(x => x.WorkPlanLineId);
        builder.HasIndex(x => x.CertificateId);
        builder.HasIndex(x => x.IbysQueryId);
    }
}
