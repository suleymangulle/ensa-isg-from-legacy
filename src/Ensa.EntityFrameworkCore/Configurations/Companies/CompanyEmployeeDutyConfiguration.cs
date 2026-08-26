using Ensa.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyEmployeeDuty"/> table mapping (OHS duty assigned to an employee).
/// </summary>
public class CompanyEmployeeDutyConfiguration : IEntityTypeConfiguration<CompanyEmployeeDuty>
{
    public void Configure(EntityTypeBuilder<CompanyEmployeeDuty> builder)
    {
        builder.ToTable("CompanyEmployeeDuty");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CompanyEmployeeId, x.IsActive });
        builder.HasIndex(x => x.DutyId);
    }
}
