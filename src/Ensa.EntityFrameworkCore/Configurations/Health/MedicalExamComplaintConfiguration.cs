using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>Anamnesis/complaint line on an examination form.</summary>
public class MedicalExamComplaintConfiguration : IEntityTypeConfiguration<MedicalExamComplaint>
{
    public void Configure(EntityTypeBuilder<MedicalExamComplaint> builder)
    {
        builder.ToTable("MedicalExamComplaint");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Text);

        // At most one row per complaint type per form.
        builder.HasIndex(x => new { x.TenantId, x.MedicalExaminationFormId, x.ComplaintType })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.MedicalExaminationFormId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
