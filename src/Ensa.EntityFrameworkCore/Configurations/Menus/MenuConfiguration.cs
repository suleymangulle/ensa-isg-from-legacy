using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Menu root (the merge of the legacy <c>Menu_T</c> and <c>NewMenu_T</c>). A host reference table.
/// </summary>
public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menu");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.MenuTypeCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.UserTypeCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.OrganizationTypeId);
        builder.HasIndex(x => x.SubscriptionPlanId);

        // Main query of menu resolution: type + user type + organization type + subscription plan.
        builder.HasIndex(x => new { x.MenuTypeCode, x.UserTypeCode, x.OrganizationTypeId, x.SubscriptionPlanId });
    }
}
