using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Control measure taken or planned for an identified hazard.</summary>
public class ControlMeasureConfiguration : IEntityTypeConfiguration<ControlMeasure>
{
    public void Configure(EntityTypeBuilder<ControlMeasure> builder)
    {
        builder.ToTable("ControlMeasure");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Measure)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.HasIndex(x => x.IdentifiedHazardId);

        // Due-date tracking for open (incomplete) control measures.
        builder.HasIndex(x => new { x.TenantId, x.IsCompleted, x.DeadlineDate });

        builder.HasIndex(x => x.OwnerCompanyEmployeeId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
