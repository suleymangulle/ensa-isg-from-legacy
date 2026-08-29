using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Risk assessment report header table.</summary>
public class RiskAssessmentReportConfiguration : IEntityTypeConfiguration<RiskAssessmentReport>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentReport> builder)
    {
        builder.ToTable("RiskAssessmentReport");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.WorkplaceTitle)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.BusinessActivity)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.WorkplaceAddress)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.WorkplacePhoneNumber)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.WorkplaceDepartments)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.MachineryAndEquipment)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.HazardousArticles)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.WasteOperations)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.Employer)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.SpecialistFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.PhysicianFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Tracking expired reports; deleted rows do not enter the index.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.ValidityDate })
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.TenantId, x.ApprovalStatus });
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.SpecialistUserId);
        builder.HasIndex(x => x.PhysicianUserId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
