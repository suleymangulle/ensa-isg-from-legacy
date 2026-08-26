using Ensa.Domain.Shared;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Tenancy;

/// <summary>
/// Sales prospect (potential customer) record. A host CRM table; it carries no <c>TenantId</c>.
/// </summary>
public class ProspectOrganizationConfiguration : IEntityTypeConfiguration<ProspectOrganization>
{
    public void Configure(EntityTypeBuilder<ProspectOrganization> builder)
    {
        builder.ToTable("ProspectOrganization");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // The national id is personal data; the legacy system stored it in clear text, the new model stores it encrypted.
        builder.Property(x => x.NationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.OrganizationTitle)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Email)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.Note)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.ContractNote)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.VatRate).HasPrecision(18, 2);
        builder.Property(x => x.GrossWithVatPrice).HasPrecision(18, 2);

        builder.HasIndex(x => x.SubscriptionPlanId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.SalesRepId);
        builder.HasIndex(x => x.ReferenceCompanyId);
        builder.HasIndex(x => x.AssignmentLogId);
        builder.HasIndex(x => new { x.ContractStatus, x.IsActive });
    }
}
