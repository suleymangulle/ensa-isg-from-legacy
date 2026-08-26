using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Participants of the risk assessment team.</summary>
public class RiskAssessmentParticipantConfiguration : IEntityTypeConfiguration<RiskAssessmentParticipant>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentParticipant> builder)
    {
        builder.ToTable("RiskAssessmentParticipant");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Title)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => new { x.RiskAssessmentReportId, x.ParticipantType });
        builder.HasIndex(x => x.CompanyEmployeeId);
    }
}
