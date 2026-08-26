using Ensa.Domain.Shared;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Tenancy;

/// <summary>
/// Sales representative. A host record; because the sign-in details live on the <c>User</c> (Identity) side,
/// this table has no password field.
/// </summary>
public class SalesRepConfiguration : IEntityTypeConfiguration<SalesRep>
{
    public void Configure(EntityTypeBuilder<SalesRep> builder)
    {
        builder.ToTable("SalesRep");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // A system user can be linked to at most one sales representative record.
        builder.HasIndex(x => x.UserId)
               .IsUnique()
               .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => new { x.SalesRepType, x.IsActive });
    }
}
