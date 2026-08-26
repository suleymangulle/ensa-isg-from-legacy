using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="AssignedSpecialist"/> table mapping (specialist/physician assigned to a company).
/// </summary>
public class AssignedSpecialistConfiguration : IEntityTypeConfiguration<AssignedSpecialist>
{
    public void Configure(EntityTypeBuilder<AssignedSpecialist> builder)
    {
        builder.ToTable("AssignedSpecialist");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sid)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Guid);

        builder.Property(x => x.OhsProfApprovalGuid)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Guid);

        builder.HasIndex(x => x.CompanyId);

        // "Companies served by this specialist/physician" screen.
        builder.HasIndex(x => new { x.UserId, x.IsActive });

        // The same user can hold a given duty at a given company only once while active.
        builder.HasIndex(x => new { x.CompanyId, x.UserId, x.StaffRole })
               .IsUnique()
               .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
    }
}
