using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary>
/// <see cref="InvoiceTemplate"/> table mapping.
/// <para>
/// <see cref="InvoiceTemplate.Design"/> holds the report design content; its length cannot be capped, so it
/// is mapped as <c>nvarchar(max)</c>.
/// </para>
/// </summary>
public class InvoiceTemplateConfiguration : IEntityTypeConfiguration<InvoiceTemplate>
{
    public void Configure(EntityTypeBuilder<InvoiceTemplate> builder)
    {
        builder.ToTable("InvoiceTemplate");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DesignName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Design)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        // Finding the default template for a module.
        builder.HasIndex(x => new { x.TenantId, x.ModuleType, x.OnValue });
    }
}
