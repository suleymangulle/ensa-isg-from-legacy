using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyEmployee"/> table mapping.
/// </summary>
public class CompanyEmployeeConfiguration : IEntityTypeConfiguration<CompanyEmployee>
{
    public void Configure(EntityTypeBuilder<CompanyEmployee> builder)
    {
        builder.ToTable("CompanyEmployee");
        builder.HasKey(x => x.Id);

        // ---------------- Identity ----------------

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.FatherName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.MotherName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Encrypted column — WHERE/UNIQUE keep working because the AES is deterministic.
        builder.Property(x => x.NationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.BirthLocation)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---------------- Contact ----------------

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Gsm)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Email)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.Property(x => x.HomeAddress)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.EmergencyPerson)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.EmergencyPersonPhone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        // ---------------- Employment ----------------

        builder.Property(x => x.Duty)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Occupation)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.AssignedDepartmentName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---------------- Pre-employment examination ----------------

        builder.Property(x => x.PreEmploymentExamination)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.PreEmploymentExaminationPerformedBy)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---------------- IBYS ----------------

        builder.Property(x => x.WorkMethodCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.WorkEnvironmentCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.WorkEquipmentCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- System ----------------

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // ---------------- Indexes ----------------

        // The same national id cannot appear twice within the same company.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.NationalId })
               .IsUnique()
               .HasFilter("[NationalId] IS NOT NULL AND [IsDeleted] = 0");

        // Company employee list (filtered by active/inactive).
        builder.HasIndex(x => new { x.CompanyId, x.IsActive });

        // The "is this person already active at another company?" rule relies on this index.
        // Without it the rule turns into a table scan on every call.
        builder.HasIndex(x => new { x.TenantId, x.NationalId, x.IsActive });

        // Foreign key indexes.
        builder.HasIndex(x => x.AssignedDepartmentId);
        builder.HasIndex(x => x.OccupationCodeId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.PreEmploymentExaminationDocumentId);
    }
}
