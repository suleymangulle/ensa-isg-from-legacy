using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="Form"/> table mapping (downloadable form/template).
/// </summary>
public class FormConfiguration : IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> builder)
    {
        builder.ToTable("Form");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FormName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // Form list per category (active rows).
        builder.HasIndex(x => new { x.CategoryId, x.IsActive });

        builder.HasIndex(x => x.DocumentId);
    }
}
