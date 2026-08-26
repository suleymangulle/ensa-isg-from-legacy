using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// Permission definition. A host reference table.
/// <para>
/// <see cref="Permission.ParentPermissionId"/> is a self-referencing foreign key; per the architecture no
/// <c>HasOne/WithMany</c> is configured, the column is only indexed.
/// </para>
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission");
        builder.HasKey(x => x.Id);

        // Full name matching the EnsaPermissions constants ("Ensa.Company.Create").
        builder.Property(x => x.PermissionTarget)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.PermissionName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.PermissionDescription)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.RedMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.HasIndex(x => x.ParentPermissionId);
        builder.HasIndex(x => x.PermissionTarget).IsUnique();
        builder.HasIndex(x => new { x.PermissionType, x.SortOrder });
    }
}
