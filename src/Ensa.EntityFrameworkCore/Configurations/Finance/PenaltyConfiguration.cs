using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary>
/// <see cref="Penalty"/> table mapping.
/// <para>A HOST reference library (it does not implement <c>IMultiTenant</c>) — there is no <c>TenantId</c> index.</para>
/// <para>The amounts do NOT live in this table but in the <see cref="PenaltyAmount"/> child table.</para>
/// </summary>
public class PenaltyConfiguration : IEntityTypeConfiguration<Penalty>
{
    public void Configure(EntityTypeBuilder<Penalty> builder)
    {
        builder.ToTable("Penalty");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TreeNodeCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.LawArticle)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.PenaltyArticle)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.LawArticleReferencedOffence)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // Navigating the legislation/regulation tree.
        builder.HasIndex(x => x.TreeNodeCode);

        builder.HasIndex(x => x.IsActive);
    }
}
