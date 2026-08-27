using Ensa.Domain.Reports;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Reports;

/// <summary>
/// <see cref="YearEndReviewLine"/> table mapping.
/// <para>
/// Sub-activities form a self-referencing tree through <see cref="YearEndReviewLine.ParentLineId"/>; because
/// navigation properties are banned, NO foreign key relationship is configured — the column is only indexed.
/// </para>
/// </summary>
public class YearEndReviewLineConfiguration : IEntityTypeConfiguration<YearEndReviewLine>
{
    public void Configure(EntityTypeBuilder<YearEndReviewLine> builder)
    {
        builder.ToTable("YearEndReviewLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DateText)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);


        builder.Property(x => x.Work)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.PersonVeTitle)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.RepeatCount)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.UsedMethod)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.ResultVeComment)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // Reading the tree level by level in a stable order (also served by the report foreign key index).
        builder.HasIndex(x => new { x.YearEndReviewReportId, x.ParentLineId, x.OrderNo });

        // Finding child lines from their parent line.
        builder.HasIndex(x => x.ParentLineId);
    }
}
