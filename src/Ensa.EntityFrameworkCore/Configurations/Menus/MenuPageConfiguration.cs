using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Page → menu mapping. A host reference table.
/// </summary>
public class MenuPageConfiguration : IEntityTypeConfiguration<MenuPage>
{
    public void Configure(EntityTypeBuilder<MenuPage> builder)
    {
        builder.ToTable("MenuPage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.MenuCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Because the page address is searched on, the length is capped at the Url length (512);
        // nvarchar(512) = 1024 bytes, below SQL Server's nonclustered index limit of 1700.
        builder.Property(x => x.PageUrl)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        builder.Property(x => x.SettlementCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.MenuCode);
        builder.HasIndex(x => x.PageUrl);
        builder.HasIndex(x => new { x.ProjectCode, x.SettlementCode, x.IsActive });
    }
}
