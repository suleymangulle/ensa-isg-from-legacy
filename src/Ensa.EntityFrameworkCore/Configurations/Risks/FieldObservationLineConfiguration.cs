using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Non-conformity line of a field observation report.</summary>
public class FieldObservationLineConfiguration : IEntityTypeConfiguration<FieldObservationLine>
{
    public void Configure(EntityTypeBuilder<FieldObservationLine> builder)
    {
        builder.ToTable("FieldObservationLine");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NonConformity)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Measures)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Owner)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.FieldObservationReportId);
        builder.HasIndex(x => x.DeadlineDate);
        builder.HasIndex(x => x.OwnerCompanyEmployeeId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
