using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="NumberSequence"/> table mapping (document number counter).
/// </summary>
public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("NumberSequence");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Counter key. The concurrency lock used while generating numbers (selecting the single
        // row with UPDLOCK/ROWLOCK) relies on this unique index — if duplicate rows could be
        // created, the same number would be handed out twice.
        builder.HasIndex(x => new { x.TenantId, x.ScopeId, x.Type }).IsUnique();

        builder.HasIndex(x => x.ScopeId);
    }
}
