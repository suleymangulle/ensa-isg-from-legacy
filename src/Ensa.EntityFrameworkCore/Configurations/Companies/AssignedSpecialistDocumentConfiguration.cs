using Ensa.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="AssignedSpecialistDocument"/> table mapping (assignment document).
/// </summary>
public class AssignedSpecialistDocumentConfiguration : IEntityTypeConfiguration<AssignedSpecialistDocument>
{
    public void Configure(EntityTypeBuilder<AssignedSpecialistDocument> builder)
    {
        builder.ToTable("AssignedSpecialistDocument");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.AssignedSpecialistId, x.IsActive });
        builder.HasIndex(x => x.DocumentId);
    }
}
