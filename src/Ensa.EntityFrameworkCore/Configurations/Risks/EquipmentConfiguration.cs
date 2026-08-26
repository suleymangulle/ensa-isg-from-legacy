using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Work equipment subject to periodic inspection.</summary>
public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EquipmentName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.ExaminationReport)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.ExaminationPerformedBy)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.EquipmentType });

        // Tracking equipment whose inspection is overdue (IEquipmentRepository uses this index).
        builder.HasIndex(x => x.NextExaminationDate)
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.ExaminationReportDocumentId);
        builder.HasIndex(x => x.PeriodId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
