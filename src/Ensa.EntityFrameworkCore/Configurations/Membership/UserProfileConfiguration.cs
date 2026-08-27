using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfile");
        builder.HasKey(x => x.Id);

        // One profile per account.
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Deterministic, which is what makes WHERE and UNIQUE work on an encrypted column.
        builder.Property(x => x.NationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.Color)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Same shape as the index it replaces on User: one identity number per tenant, ignoring
        // deleted rows and rows that never had one.
        builder.HasIndex(x => new { x.TenantId, x.NationalId })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0 AND [NationalId] IS NOT NULL");

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.PhotoDocumentId);
    }
}
