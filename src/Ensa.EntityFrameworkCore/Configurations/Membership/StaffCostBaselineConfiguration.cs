using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// User cost/baseline analysis record (period snapshot). A tenant-owned record.
/// </summary>
public class StaffCostBaselineConfiguration : IEntityTypeConfiguration<StaffCostBaseline>
{
    public void Configure(EntityTypeBuilder<StaffCostBaseline> builder)
    {
        builder.ToTable("StaffCostBaseline");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Salary).HasPrecision(18, 2);
        builder.Property(x => x.SsiAmount).HasPrecision(18, 2);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.OfficeId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
