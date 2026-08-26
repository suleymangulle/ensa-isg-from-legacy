using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Document / inspection certificate attached to a piece of equipment.</summary>
public class EquipmentDocumentConfiguration : IEntityTypeConfiguration<EquipmentDocument>
{
    public void Configure(EntityTypeBuilder<EquipmentDocument> builder)
    {
        builder.ToTable("EquipmentDocument");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.ExaminationPerformedBy)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.EquipmentId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.ValidityDate);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.EquipmentDocumentTypeId);
        builder.HasIndex(x => x.ActivityId);
        builder.HasIndex(x => x.WorkPlanLineId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
