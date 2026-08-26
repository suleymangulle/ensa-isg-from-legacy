using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="NewsletterSubscriber"/> table mapping.
/// <para>A host (tenant-less) table.</para>
/// </summary>
public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.ToTable("NewsletterSubscriber");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        // The same e-mail address cannot subscribe twice.
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
