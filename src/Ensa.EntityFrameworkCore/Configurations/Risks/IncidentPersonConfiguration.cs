using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Person involved in an incident (affected / witness / responder).</summary>
public class IncidentPersonConfiguration : IEntityTypeConfiguration<IncidentPerson>
{
    public void Configure(EntityTypeBuilder<IncidentPerson> builder)
    {
        builder.ToTable("IncidentPerson");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName);

        builder.HasIndex(x => new { x.IncidentId, x.PersonType });
        builder.HasIndex(x => x.CompanyEmployeeId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
