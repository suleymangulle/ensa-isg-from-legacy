using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="Message"/> table mapping.
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // Ensa.Domain.Communication.Message (user-to-user messaging) uses the "Message" table.
        // This table is the dictionary of notification text TEMPLATES; the table names were split apart.
        builder.ToTable("MessageTemplate");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Text)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.CssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasIndex(x => x.MessageTemplateTypeId);
    }
}
