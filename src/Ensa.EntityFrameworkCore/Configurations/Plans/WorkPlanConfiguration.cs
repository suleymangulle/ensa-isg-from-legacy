using Ensa.Domain.Plans;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Plans;

/// <summary>
/// <see cref="WorkPlan"/> table mapping.
/// <para>
/// DESIGN NOTE — the "one active plan per company per year" rule CANNOT be expressed with a database index:
/// the entity has no separate <c>Year</c> column, the year is derived from
/// <see cref="WorkPlan.StartDate"/>, and SQL Server filtered indexes do not support computed expressions
/// (<c>YEAR(...)</c>). The rule is therefore enforced at the application level inside
/// <c>WorkPlanManager</c>.
/// </para>
/// </summary>
public class WorkPlanConfiguration : IEntityTypeConfiguration<WorkPlan>
{
    public void Configure(EntityTypeBuilder<WorkPlan> builder)
    {
        builder.ToTable("WorkPlan");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RevisionNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.DocumentNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- Indexes ----------------

        // The company's plan history and the search for the active plan of a given year.
        builder.HasIndex(x => new { x.CompanyId, x.StartDate });

        // Export queue scan for the external system.
        builder.HasIndex(x => new { x.TenantId, x.IsTransferred });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.SpecialistUserId);
        builder.HasIndex(x => x.PhysicianUserId);
        builder.HasIndex(x => x.ApproverUserId);
        builder.HasIndex(x => x.ControlItemListId);
        builder.HasIndex(x => x.PreviousPlanId);
    }
}
