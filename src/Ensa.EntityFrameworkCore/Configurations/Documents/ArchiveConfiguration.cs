using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="Archive"/> table mapping (per-module document archive).
/// </summary>
public class ArchiveConfiguration : IEntityTypeConfiguration<Archive>
{
    public void Configure(EntityTypeBuilder<Archive> builder)
    {
        builder.ToTable("Archive");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.ModuleDescription)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        // Polymorphic module link — an index is mandatory because there is no real foreign key.
        // LineId is meaningless on its own (it identifies a record only together with
        // ModuleType+ModuleId), so it is part of the composite index rather than a separate one.
        builder.HasIndex(x => new { x.ModuleType, x.ModuleId, x.LineId });

        // Company archive screen: listing broken down by year and month.
        builder.HasIndex(x => new { x.CompanyId, x.Year, x.Month });

        builder.HasIndex(x => new { x.TenantId, x.CompanyId });

        builder.HasIndex(x => x.DocumentId);

        // PreviousAddedByUserId is deliberately left unindexed: it is not a live foreign key but
        // only a legacy trace kept during data migration, and it is never used as a query filter.
    }
}
