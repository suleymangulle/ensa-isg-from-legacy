using Ensa.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyEmployeeDutyDocument"/> table mapping (duty certificate).
/// </summary>
public class CompanyEmployeeDutyDocumentConfiguration : IEntityTypeConfiguration<CompanyEmployeeDutyDocument>
{
    public void Configure(EntityTypeBuilder<CompanyEmployeeDutyDocument> builder)
    {
        builder.ToTable("CompanyEmployeeDutyDocument");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CompanyEmployeeDutyId, x.IsActive });
        builder.HasIndex(x => x.DocumentId);
    }
}
