using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Work accident / near miss / occupational disease incident record.</summary>
public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incident");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Expression)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.SupervisorFullName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.IncidentDate });

        // Social Security Act no. 5510 art. 13: detecting work accidents that were never reported to SGK.
        builder.HasIndex(x => new { x.IncidentType, x.SsiNotificationDate })
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.DepartmentId);
        builder.HasIndex(x => x.UnitSupervisorId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
