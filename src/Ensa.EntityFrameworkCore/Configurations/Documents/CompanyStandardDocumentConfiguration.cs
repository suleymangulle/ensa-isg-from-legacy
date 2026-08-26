using Ensa.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="CompanyStandardDocument"/> table mapping.
/// </summary>
public class CompanyStandardDocumentConfiguration : IEntityTypeConfiguration<CompanyStandardDocument>
{
    public void Configure(EntityTypeBuilder<CompanyStandardDocument> builder)
    {
        builder.ToTable("CompanyStandardDocument");
        builder.HasKey(x => x.Id);

        // The company's checklist per document type.
        // Not UNIQUE, because more than one revision of the same document type may be submitted.
        builder.HasIndex(x => new { x.CompanyId, x.StandardDocumentId });

        builder.HasIndex(x => x.StandardDocumentId);
        builder.HasIndex(x => x.DocumentId);
    }
}
