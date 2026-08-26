using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// A permission included in a subscription plan — a mandatory gate in the permission calculation. A host definition.
/// </summary>
public class SubscriptionPlanPermissionConfiguration : IEntityTypeConfiguration<SubscriptionPlanPermission>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanPermission> builder)
    {
        builder.ToTable("SubscriptionPlanPermission");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PermissionId);

        // No separate index for the SubscriptionPlanId foreign key: it is the LEADING column of this composite index.
        builder.HasIndex(x => new { x.SubscriptionPlanId, x.PermissionId }).IsUnique();
    }
}
