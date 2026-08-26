using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// User type reference table. A host table; it does not implement soft delete.
/// </summary>
public class UserTypeConfiguration : IEntityTypeConfiguration<UserType>
{
    public void Configure(EntityTypeBuilder<UserType> builder)
    {
        builder.ToTable("UserType");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.IconCssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.StaffRole);
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
