using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// Licence of the e-signature component.
/// <para>
/// A host table. <see cref="ESignatureLicense.License"/> is confidential and is stored encrypted.
/// </para>
/// </summary>
public class ESignatureLicenseConfiguration : IEntityTypeConfiguration<ESignatureLicense>
{
    public void Configure(EntityTypeBuilder<ESignatureLicense> builder)
    {
        builder.ToTable("ESignatureLicense");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.License)
               .IsRequired()
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Text);

        // Finding the currently valid licence.
        builder.HasIndex(x => new { x.IsActive, x.ValidityDate });
    }
}
