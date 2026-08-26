using Ensa.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyCheckLine"/> table mapping (result of a checklist item).
/// </summary>
public class CompanyCheckLineConfiguration : IEntityTypeConfiguration<CompanyCheckLine>
{
    public void Configure(EntityTypeBuilder<CompanyCheckLine> builder)
    {
        builder.ToTable("CompanyCheckLine");
        builder.HasKey(x => x.Id);

        // The same control item cannot appear twice under the same header.
        // The base class carries no soft delete, so no filter is needed.
        builder.HasIndex(x => new { x.CompanyControlItemId, x.ControlItemId }).IsUnique();

        builder.HasIndex(x => x.ControlItemId);
    }
}
