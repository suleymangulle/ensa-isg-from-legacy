using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="Log"/> table mapping.
/// <para>
/// A write-and-read transaction table; the most frequent query is "tenant + date range", so that pair is the
/// primary index.
/// </para>
/// </summary>
public class LogConfiguration : IEntityTypeConfiguration<Log>
{
    public void Configure(EntityTypeBuilder<Log> builder)
    {
        builder.ToTable("Log");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PageName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.MethodName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Message)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.Parameters)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        // The log viewing screen and the archiving job both use this index.
        builder.HasIndex(x => new { x.TenantId, x.CreationTime });

        builder.HasIndex(x => x.UserId);
    }
}
