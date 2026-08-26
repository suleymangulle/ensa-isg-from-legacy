using Ensa.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyTrainingProgressMode"/> table mapping.
/// <para>There is a single settings record per company; this 1-1 guarantee is enforced at the database level.</para>
/// </summary>
public class CompanyTrainingProgressModeConfiguration : IEntityTypeConfiguration<CompanyTrainingProgressMode>
{
    public void Configure(EntityTypeBuilder<CompanyTrainingProgressMode> builder)
    {
        builder.ToTable("CompanyTrainingProgressMode");
        builder.HasKey(x => x.Id);

        // At most one progress mode setting per company.
        // The base class carries no soft delete, so no filter is needed.
        builder.HasIndex(x => x.CompanyId).IsUnique();

        builder.HasIndex(x => x.UserId);
    }
}
