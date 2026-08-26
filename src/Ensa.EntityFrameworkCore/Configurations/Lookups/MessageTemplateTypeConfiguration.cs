using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="MessageTemplateType"/> table mapping.
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class MessageTemplateTypeConfiguration : IEntityTypeConfiguration<MessageTemplateType>
{
    public void Configure(EntityTypeBuilder<MessageTemplateType> builder)
    {
        builder.ToTable("MessageTemplateType");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.CssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
