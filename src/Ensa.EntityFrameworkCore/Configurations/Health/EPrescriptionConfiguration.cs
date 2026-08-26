using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// e-prescription header record.
/// <para>
/// <see cref="EPrescription.PatientNationalId"/> is stored encrypted but DETERMINISTICALLY, so it can be
/// indexed and searched for equality (see <see cref="EncryptedStringConverter"/>).
/// </para>
/// </summary>
public class EPrescriptionConfiguration : IEntityTypeConfiguration<EPrescription>
{
    public void Configure(EntityTypeBuilder<EPrescription> builder)
    {
        builder.ToTable("EPrescription");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientNationalId)
               .IsRequired()
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.Description!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.EPrescriptionCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ProtocolNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ResultCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ResultMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.WarningMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.HasIndex(x => new { x.TenantId, x.PatientNationalId });
        builder.HasIndex(x => new { x.TenantId, x.EPrescriptionCode });
        builder.HasIndex(x => x.PatientCompanyEmployeeId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
