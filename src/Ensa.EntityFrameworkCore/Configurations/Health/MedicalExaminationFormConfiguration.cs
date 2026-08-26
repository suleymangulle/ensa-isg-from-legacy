using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// Health surveillance examination form (Annex 2) header and result record.
/// <para>
/// <b>ENCRYPTION DECISION (ADR-005).</b> <see cref="EncryptedStringConverter"/> is only for <c>string</c>
/// columns. Fields that were stored as encrypted text in the legacy system but are numeric or date typed here
/// (<c>Boy</c>, <c>WeightKg</c>, <c>BodyMassIndex</c>, <c>BloodPressure*</c>, <c>PulseRate</c>, the dates) are
/// deliberately not encrypted; they are protected with TDE at the database level. Otherwise numeric
/// comparison, sorting and trend reports would become impossible.
/// </para>
/// </summary>
public class MedicalExaminationFormConfiguration : IEntityTypeConfiguration<MedicalExaminationForm>
{
    public void Configure(EntityTypeBuilder<MedicalExaminationForm> builder)
    {
        builder.ToTable("MedicalExaminationForm");
        builder.HasKey(x => x.Id);

        // ---- Anthropometry (not encrypted — see the class remarks) ----
        builder.Property(x => x.WeightKg).HasPrecision(6, 2);
        builder.Property(x => x.BodyMassIndex).HasPrecision(6, 2);

        // ---- Free text carrying personal health data: ENCRYPTED ----
        builder.Property(x => x.ChronicIllnessDeclaration!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.OpinionDescription!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.Recommendations!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Note);

        // ---- IBYS submission fields ----
        builder.Property(x => x.IbysStatusMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.IbysGroupCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.IbysOccupationCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Comma-separated code lists.
        builder.Property(x => x.IbysWorkEnvironmentCodes)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.IbysWorkArrangementCodes)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.IbysWorkEquipmentCodes)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.Source)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName);

        // ---- Indexes ----
        builder.HasIndex(x => new { x.TenantId, x.CompanyEmployeeId, x.ExaminationDate });
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.ValidityDate });
        builder.HasIndex(x => x.IbysStatus);
        builder.HasIndex(x => x.PhysicianUserId);
        builder.HasIndex(x => x.IbysQueryId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
