using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="DepartmentDocument"/> table mapping (department measurement/inspection document).
/// </summary>
public class DepartmentDocumentConfiguration : IEntityTypeConfiguration<DepartmentDocument>
{
    public void Configure(EntityTypeBuilder<DepartmentDocument> builder)
    {
        builder.ToTable("DepartmentDocument");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.ExaminationPerformedBy)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.WorkplaceDepartmentId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.ActivityId);
        builder.HasIndex(x => x.WorkPlanLineId);

        // Warnings for documents whose validity has expired.
        builder.HasIndex(x => new { x.TenantId, x.ValidityDate });
    }
}
