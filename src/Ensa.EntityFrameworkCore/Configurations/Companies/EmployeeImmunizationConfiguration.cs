using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="EmployeeImmunization"/> table mapping (vaccination record).
/// </summary>
public class EmployeeImmunizationConfiguration : IEntityTypeConfiguration<EmployeeImmunization>
{
    public void Configure(EntityTypeBuilder<EmployeeImmunization> builder)
    {
        builder.ToTable("EmployeeImmunization");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        // Not UNIQUE, because the same vaccine may have more than one dose.
        builder.HasIndex(x => x.CompanyEmployeeId);
    }
}
