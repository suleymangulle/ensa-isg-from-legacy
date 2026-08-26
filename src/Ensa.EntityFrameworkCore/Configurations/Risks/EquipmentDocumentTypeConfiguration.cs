using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Organization-specific definition list (lookup) for equipment documents.</summary>
public class EquipmentDocumentTypeConfiguration : IEntityTypeConfiguration<EquipmentDocumentType>
{
    public void Configure(EntityTypeBuilder<EquipmentDocumentType> builder)
    {
        builder.ToTable("EquipmentDocumentType");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => new { x.TenantId, x.IsActive, x.SortOrder });
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
