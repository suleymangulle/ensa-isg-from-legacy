using Ensa.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Employee assigned to an emergency response team.</summary>
public class EmergencyTeamMemberConfiguration : IEntityTypeConfiguration<EmergencyTeamMember>
{
    public void Configure(EntityTypeBuilder<EmergencyTeamMember> builder)
    {
        builder.ToTable("EmergencyTeamMember");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.EmergencyActionPlanId, x.TeamType });
        builder.HasIndex(x => x.CompanyEmployeeId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
