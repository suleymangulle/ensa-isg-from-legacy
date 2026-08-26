using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="Company"/> table mapping.
/// <para>
/// The headquarters/branch link is a self-reference through <see cref="Company.HeadquarterCompanyId"/>;
/// because navigation properties are banned, NO foreign key relationship is configured — the column is only
/// indexed.
/// </para>
/// </summary>
public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Company");
        builder.HasKey(x => x.Id);

        // ---------------- Identity ----------------

        builder.Property(x => x.CompanyName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Sid)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Guid);

        builder.Property(x => x.SsiNumber)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.TaxTaxOffice)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Encrypted column — deterministic AES; querying and uniqueness are preserved.
        builder.Property(x => x.TaxNumber!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.TaxNo);

        builder.Property(x => x.EmployerName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.EmployerMobilePhone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.BusinessActivity)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        // ---------------- Headquarters / branch ----------------

        builder.Property(x => x.BranchName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.BranchContact)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.BranchContactGsm)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        // ---------------- Contact / address ----------------

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.InvoiceAddress)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        // Geographic coordinate — not money; 9,6 gives roughly 11 cm of resolution.
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Fax)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Gsm)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Email)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        // Semicolon-separated CC list — can be longer than a single e-mail address.
        builder.Property(x => x.Cc)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.WebUrl)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        builder.Property(x => x.AuthorizedPerson)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.AuthorizedPersonPhone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.AuthorizedPersonEmail)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.Property(x => x.FinanceOwner)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.FinanceOwnerGsm)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        // ---------------- Financial ----------------

        builder.Property(x => x.MonthlyFeeOfficial).HasPrecision(18, 2);
        builder.Property(x => x.MonthlyFeeTotal).HasPrecision(18, 2);
        builder.Property(x => x.SpecialistFee).HasPrecision(18, 2);
        builder.Property(x => x.PhysicianFee).HasPrecision(18, 2);
        builder.Property(x => x.InvoiceAmount).HasPrecision(18, 2);
        builder.Property(x => x.InvoiceAmountKh).HasPrecision(18, 2);
        builder.Property(x => x.GrContractAmount).HasPrecision(18, 2);
        builder.Property(x => x.PayableDigit).HasPrecision(18, 2);

        // ---------------- Notes ----------------

        builder.Property(x => x.Notes)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.WarningNote)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.NoteRecordedBy)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---------------- Indexes ----------------

        // SGK registration numbers are unique within a tenant; empty and deleted rows are excluded.
        builder.HasIndex(x => new { x.TenantId, x.SsiNumber })
               .IsUnique()
               .HasFilter("[SsiNumber] IS NOT NULL AND [IsDeleted] = 0");

        // Customer lists: active companies that have no organization record.
        builder.HasIndex(x => new { x.TenantId, x.IsActive, x.IsOrganizationRecord });

        // Listing per office.
        builder.HasIndex(x => new { x.TenantId, x.OfficeId });

        // Search and sort by title.
        builder.HasIndex(x => x.CompanyName);

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.HeadquarterCompanyId);
        builder.HasIndex(x => x.GroupCorporateId);
        builder.HasIndex(x => x.OccupationCodeId);
        builder.HasIndex(x => x.OrganizationTypeId);
        builder.HasIndex(x => x.SubscriptionPlanId);
        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.QuarterId);
        builder.HasIndex(x => x.NeighborhoodId);
        builder.HasIndex(x => x.OfficeId);
        builder.HasIndex(x => x.LogoDocumentId);
    }
}
