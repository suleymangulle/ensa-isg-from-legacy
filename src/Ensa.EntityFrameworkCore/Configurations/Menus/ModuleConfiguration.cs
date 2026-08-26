using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Application module (hierarchical). A host reference table.
/// <para>
/// <see cref="Module.ParentModuleId"/> is a self-referencing foreign key; no relationship is configured, the
/// column is only indexed.
/// </para>
/// </summary>
public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("Module");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // No separate index for the ParentModuleId foreign key: it is the LEADING column of this composite index.
        builder.HasIndex(x => new { x.ParentModuleId, x.IsActive, x.SortOrder });
    }
}
