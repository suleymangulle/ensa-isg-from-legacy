using Ensa.Domain.Plans;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Plans;

/// <summary>
/// <see cref="Activity"/> table mapping.
/// <para>
/// This is a MIXED (host + tenant) library table: rows with <c>TenantId = null</c> are the shared activity
/// definitions available to every organization.
/// </para>
/// <para>
/// The hierarchy is a self-reference through <see cref="Activity.ParentActivityId"/>; because navigation
/// properties are banned, NO foreign key relationship is configured — the column is only indexed.
/// </para>
/// </summary>
public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activity");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActivityCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ActivityName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // Target table name of the polymorphic reference.
        builder.Property(x => x.RelatedTable)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---------------- Indexes ----------------

        // Search by code. NOT UNIQUE: the legacy data contains duplicate codes, so
        // uniqueness is enforced at the application level.
        builder.HasIndex(x => new { x.TenantId, x.ActivityCode });

        // Catalogue listing: active activities.
        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        // Reverse lookup through the polymorphic reference.
        builder.HasIndex(x => new { x.RelatedTable, x.RelationId });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.ParentActivityId);
        builder.HasIndex(x => x.ActivityGroupId);
        builder.HasIndex(x => x.PeriodId);
    }
}
