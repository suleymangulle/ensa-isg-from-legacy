using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Corrective / preventive action record.</summary>
public class CorrectiveActionConfiguration : IEntityTypeConfiguration<CorrectiveAction>
{
    public void Configure(EntityTypeBuilder<CorrectiveAction> builder)
    {
        builder.ToTable("CorrectiveAction");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Finding)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Recommendation)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Result)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Source)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Owner)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.OperationResult });

        // Tracking open corrective actions past their due date.
        builder.HasIndex(x => x.DeadlineDate)
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.FieldObservationLineId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.OwnerCompanyEmployeeId);
        builder.HasIndex(x => x.FindingDocumentId);
        builder.HasIndex(x => x.ResultDocumentId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
