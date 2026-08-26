using Ensa.Domain.Shared;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Tenancy;

/// <summary>
/// Office / branch of an organization. A tenant-owned record; uniqueness constraints are always
/// composite with <c>TenantId</c>.
/// </summary>
public class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.ToTable("Office");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Fax)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.AuthorizedPerson)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.AuthorizedEmail)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });

        // An organization can have only ONE head office.
        builder.HasIndex(x => x.TenantId)
               .IsUnique()
               .HasDatabaseName("IX_Office_TenantId_HeadquarterOffice")
               .HasFilter("[HeadquarterOffice] = 1 AND [IsDeleted] = 0");
    }
}
