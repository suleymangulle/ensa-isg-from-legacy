using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="SystemSetting"/> table mapping.
/// <para>
/// <see cref="SystemSetting.SettingName"/>, which was the primary key in the legacy schema, is preserved
/// here as a UNIQUE index; the primary key is the int <c>Id</c>.
/// </para>
/// <para>A host (tenant-less) system table.</para>
/// </summary>
public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSetting");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SettingName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Value)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.SettingType)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.SettingName).IsUnique();
    }
}
