using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// Immunisation (vaccination) line declared during an examination.
/// <para>
/// Although the <c>Date</c> field is marked as an "encrypted column" on the entity, it is of type
/// <see cref="System.DateTime"/>; <c>EncryptedStringConverter</c> only works for <c>string</c> (ADR-005).
/// This field is protected with TDE at the database level.
/// </para>
/// </summary>
public class MedicalExamImmunizationConfiguration : IEntityTypeConfiguration<MedicalExamImmunization>
{
    public void Configure(EntityTypeBuilder<MedicalExamImmunization> builder)
    {
        builder.ToTable("MedicalExamImmunization");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Text);

        // At most one row per vaccine type per form.
        builder.HasIndex(x => new { x.TenantId, x.MedicalExaminationFormId, x.ImmunizationType })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.MedicalExaminationFormId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
