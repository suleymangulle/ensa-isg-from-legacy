using Ensa.Domain.Menus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// User-specific menu customisation (add/hide). A tenant-owned record.
/// </summary>
public class UserMenuOverrideConfiguration : IEntityTypeConfiguration<UserMenuOverride>
{
    public void Configure(EntityTypeBuilder<UserMenuOverride> builder)
    {
        builder.ToTable("UserMenuOverride");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.MenuItemId);

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.MenuItemId }).IsUnique();
    }
}
