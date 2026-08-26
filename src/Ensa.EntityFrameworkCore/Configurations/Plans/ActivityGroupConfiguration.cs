using Ensa.Domain.Plans;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Plans;

/// <summary>
/// <see cref="ActivityGroup"/> table mapping.
/// <para>A HOST reference table (it does not implement <c>IMultiTenant</c>) — there is no <c>TenantId</c> index.</para>
/// </summary>
public class ActivityGroupConfiguration : IEntityTypeConfiguration<ActivityGroup>
{
    public void Configure(EntityTypeBuilder<ActivityGroup> builder)
    {
        builder.ToTable("ActivityGroup");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GroupName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.IsActive);
    }
}
