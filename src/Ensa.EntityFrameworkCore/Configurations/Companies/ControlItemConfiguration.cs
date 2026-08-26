using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="ControlItem"/> table mapping (checklist item definition).
/// </summary>
public class ControlItemConfiguration : IEntityTypeConfiguration<ControlItem>
{
    public void Configure(EntityTypeBuilder<ControlItem> builder)
    {
        builder.ToTable("ControlItem");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ControlItemName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.HasIndex(x => x.PeriodId);

        // Checklist screen: active items in their defined order.
        builder.HasIndex(x => new { x.TenantId, x.IsActive, x.SortOrder });
    }
}
