using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Emergency action plan header table.</summary>
public class EmergencyActionPlanConfiguration : IEntityTypeConfiguration<EmergencyActionPlan>
{
    public void Configure(EntityTypeBuilder<EmergencyActionPlan> builder)
    {
        builder.ToTable("EmergencyActionPlan");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.RegistrationNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Phone)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.TeamsChief)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.EmergencyTeam)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.WorkerRepresentative)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.SupportStaff)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.EmployerOrDeputy)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.OccupationalSafetySpecialist)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.WorkplacePhysician)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.ProtectionEmployee)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Tracking expired plans; deleted rows do not enter the index.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.ValidityDate })
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.EvacuationPlanDocumentId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
