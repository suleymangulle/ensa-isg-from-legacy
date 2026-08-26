using Ensa.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="ModuleArchiveItem"/> table mapping.
/// </summary>
public class ModuleArchiveItemConfiguration : IEntityTypeConfiguration<ModuleArchiveItem>
{
    public void Configure(EntityTypeBuilder<ModuleArchiveItem> builder)
    {
        builder.ToTable("ModuleArchiveItem");
        builder.HasKey(x => x.Id);

        // Office-level details of an archive header.
        builder.HasIndex(x => new { x.ModuleArchiveId, x.OfficeId });

        builder.HasIndex(x => x.OfficeId);
        builder.HasIndex(x => x.DocumentId);
    }
}
