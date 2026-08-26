using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// The "workplace served" record that the OHS provider reports to IBYS.
/// <para>
/// For <c>XmlData</c> / <c>SignedData</c> see <see cref="IbysQueryConfiguration"/>:
/// <c>nvarchar(max)</c> plus a directly attached encryption converter.
/// </para>
/// </summary>
public class IbysServedWorkplaceConfiguration : IEntityTypeConfiguration<IbysServedWorkplace>
{
    public void Configure(EntityTypeBuilder<IbysServedWorkplace> builder)
    {
        builder.ToTable("IbysServedWorkplace");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IbysNotificationNo!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.XmlData!)
               .HasConversion(new EncryptedStringConverter())
               .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SignedData!)
               .HasConversion(new EncryptedStringConverter())
               .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.TenantId, x.CompanyId });
        builder.HasIndex(x => x.IbysNotificationNo);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.ApproverUserId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
