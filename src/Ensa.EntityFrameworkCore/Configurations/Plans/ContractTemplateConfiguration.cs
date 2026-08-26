using Ensa.Domain.Plans;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Plans;

/// <summary>
/// <see cref="ContractTemplate"/> table mapping.
/// <para>
/// This is the draft work plan template used during the contract stage; its structure is identical to
/// <see cref="WorkPlan"/>.
/// </para>
/// </summary>
public class ContractTemplateConfiguration : IEntityTypeConfiguration<ContractTemplate>
{
    public void Configure(EntityTypeBuilder<ContractTemplate> builder)
    {
        builder.ToTable("ContractTemplate");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RevisionNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.DocumentNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- Indexes ----------------

        builder.HasIndex(x => x.CompanyId);

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.SpecialistUserId);
        builder.HasIndex(x => x.PhysicianUserId);
        builder.HasIndex(x => x.ApproverUserId);
        builder.HasIndex(x => x.ControlItemListId);
        builder.HasIndex(x => x.WorkPlanId);
    }
}
