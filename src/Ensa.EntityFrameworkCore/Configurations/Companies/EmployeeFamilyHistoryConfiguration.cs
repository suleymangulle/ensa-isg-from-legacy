using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="EmployeeFamilyHistory"/> table mapping.
/// </summary>
public class EmployeeFamilyHistoryConfiguration : IEntityTypeConfiguration<EmployeeFamilyHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeFamilyHistory> builder)
    {
        builder.ToTable("EmployeeFamilyHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.HasIndex(x => x.CompanyEmployeeId);

        // In the legacy schema each degree of kinship was its own column; the one-row-per-kinship rule is preserved.
        builder.HasIndex(x => new { x.CompanyEmployeeId, x.Relation })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");
    }
}
