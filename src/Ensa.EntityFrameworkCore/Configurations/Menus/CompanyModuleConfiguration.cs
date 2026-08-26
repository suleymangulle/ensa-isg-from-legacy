using Ensa.Domain.Menus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// A module enabled for a company. One of the TWO tenant tables in the Menus module
/// (the other being <see cref="UserMenuOverride"/>), which is why its uniqueness is composite with <c>TenantId</c>.
/// </summary>
public class CompanyModuleConfiguration : IEntityTypeConfiguration<CompanyModule>
{
    public void Configure(EntityTypeBuilder<CompanyModule> builder)
    {
        builder.ToTable("CompanyModule");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.ModuleId);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.ModuleId }).IsUnique();
    }
}
