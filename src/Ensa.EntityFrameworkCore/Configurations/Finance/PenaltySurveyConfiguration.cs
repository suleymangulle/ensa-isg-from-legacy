using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="PenaltySurvey"/> table mapping.</summary>
public class PenaltySurveyConfiguration : IEntityTypeConfiguration<PenaltySurvey>
{
    public void Configure(EntityTypeBuilder<PenaltySurvey> builder)
    {
        builder.ToTable("PenaltySurvey");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyTitle)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.FacilityName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.FacilityOwner)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.FacilityOwnerDuty)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.FacilityOwnerGsm)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.EmployerNameLastName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Fax)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Email)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.InvoiceAddress)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.TaxTaxOffice)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Tax number — deterministic AES (same approach as Company.TaxNumber).
        builder.Property(x => x.TaxNumber!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.TaxNo);

        builder.Property(x => x.SsiRegistrationNumber)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- Indexes ----------------

        // Prospect search.
        builder.HasIndex(x => new { x.TenantId, x.CompanyTitle });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.NeighborhoodId);
        builder.HasIndex(x => x.LogoDocumentId);
    }
}
