using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="WorkplaceDepartment"/> table mapping.
/// </summary>
public class WorkplaceDepartmentConfiguration : IEntityTypeConfiguration<WorkplaceDepartment>
{
    public void Configure(EntityTypeBuilder<WorkplaceDepartment> builder)
    {
        builder.ToTable("WorkplaceDepartment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DepartmentName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // A company cannot have two departments with the same name.
        builder.HasIndex(x => new { x.CompanyId, x.DepartmentName })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");
    }
}
