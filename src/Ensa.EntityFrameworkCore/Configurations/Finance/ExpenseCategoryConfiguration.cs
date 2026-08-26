using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary>
/// <see cref="ExpenseCategory"/> table mapping.
/// <para>
/// The tree is a self-reference through <see cref="ExpenseCategory.ParentExpenseCategoryId"/>; because
/// navigation properties are banned, NO foreign key relationship is configured — the column is only indexed.
/// </para>
/// </summary>
public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.HasIndex(x => x.ParentExpenseCategoryId);
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
