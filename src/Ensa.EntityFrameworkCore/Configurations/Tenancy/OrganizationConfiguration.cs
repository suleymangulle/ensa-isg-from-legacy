using Ensa.Domain.Shared;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Tenancy;

/// <summary>
/// Tenant root record. A host table (it does not implement <c>IMultiTenant</c>), so it has no
/// <c>TenantId</c> column and its indexes carry no tenant component.
/// </summary>
public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organization");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.TaxTaxOffice)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Legacy Firma_T.VergiNo was stored with [EncryptColumn]; the same confidentiality level is preserved.
        builder.Property(x => x.TaxNumber!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.TaxNo);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Email)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.Property(x => x.WebUrl)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        builder.Property(x => x.AuthorizedFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.AuthorizedPhone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.AuthorizedEmail)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.HasIndex(x => x.OrganizationTypeId);
        builder.HasIndex(x => x.SubscriptionPlanId);
        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.LogoDocumentId);

        // The code is the key of tenant resolution; deleted rows must not block a code.
        builder.HasIndex(x => x.Code)
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");
    }
}
