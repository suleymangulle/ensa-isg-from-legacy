using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="EmployeeHealthInfo"/> table mapping (1-1 with the employee).
/// </summary>
public class EmployeeHealthInfoConfiguration : IEntityTypeConfiguration<EmployeeHealthInfo>
{
    public void Configure(EntityTypeBuilder<EmployeeHealthInfo> builder)
    {
        builder.ToTable("EmployeeHealthInfo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AllergyDescription)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.ChronicIllnessDescription)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // The 1-1 guarantee is enforced at the database level. The index is filtered so that a
        // soft-deleted row does not block a new one.
        builder.HasIndex(x => x.CompanyEmployeeId)
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");
    }
}
