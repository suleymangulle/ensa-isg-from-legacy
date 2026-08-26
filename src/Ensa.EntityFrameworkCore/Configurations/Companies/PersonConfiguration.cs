using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="Person"/> table mapping (a natural person who is not a company employee).
/// </summary>
public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Person");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // The national id is stored encrypted (same policy as CompanyEmployee).
        builder.Property(x => x.NationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.FatherName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.MotherName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        // The same person cannot be registered twice within a tenant.
        builder.HasIndex(x => new { x.TenantId, x.NationalId })
               .IsUnique()
               .HasFilter("[NationalId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.NeighborhoodId);
    }
}
