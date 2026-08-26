using Ensa.Domain.Shared;
using Ensa.Domain.Tenancy;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Tenancy;

/// <summary>
/// Signed subscription contract. A host record; it carries no <c>TenantId</c> and is linked to the relevant
/// tenant record through <c>OrganizationId</c>.
/// </summary>
public class OrganizationContractConfiguration : IEntityTypeConfiguration<OrganizationContract>
{
    public void Configure(EntityTypeBuilder<OrganizationContract> builder)
    {
        builder.ToTable("OrganizationContract");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Legacy YetkiliTcNo was an [EncryptColumn]; it is still stored encrypted.
        builder.Property(x => x.AuthorizedNationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.AuthorizedName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.AuthorizedLastName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Email)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.Note)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.ContractNote)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.TotalPrice).HasPrecision(18, 2);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.SubscriptionPlanId);
        builder.HasIndex(x => x.OrganizationTypeId);
        builder.HasIndex(x => x.SalesRepId);
        builder.HasIndex(x => x.ReferenceCompanyId);
        builder.HasIndex(x => x.AssignmentLogId);
        builder.HasIndex(x => new { x.ContractStatus, x.IsActive });
    }
}
