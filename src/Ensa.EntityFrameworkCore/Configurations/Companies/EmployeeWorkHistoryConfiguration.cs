using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="EmployeeWorkHistory"/> table mapping.
/// </summary>
public class EmployeeWorkHistoryConfiguration : IEntityTypeConfiguration<EmployeeWorkHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkHistory> builder)
    {
        builder.ToTable("EmployeeWorkHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkSector)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.PerformedJob)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // The examination form reads these rows in order.
        builder.HasIndex(x => new { x.CompanyEmployeeId, x.OrderNo });
    }
}
