using Ensa.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Free-text sections of an emergency action plan.</summary>
public class EmergencyPlanSectionConfiguration : IEntityTypeConfiguration<EmergencyPlanSection>
{
    public void Configure(EntityTypeBuilder<EmergencyPlanSection> builder)
    {
        builder.ToTable("EmergencyPlanSection");
        builder.HasKey(x => x.Id);

        // Rich text / HTML content — no length limit is imposed.
        builder.Property(x => x.Content)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        // At most one record per section type per plan.
        // The index is filtered so that a soft-deleted row does not block a re-insert.
        builder.HasIndex(x => new { x.TenantId, x.EmergencyActionPlanId, x.SectionType })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.EmergencyActionPlanId, x.OrderNo });
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
