using Ensa.Domain.Shared;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Tenancy;

/// <summary>
/// Organization type reference table. A host table; because it does not implement soft delete, the
/// uniqueness index uses no <c>IsDeleted</c> filter.
/// </summary>
public class OrganizationTypeConfiguration : IEntityTypeConfiguration<OrganizationType>
{
    public void Configure(EntityTypeBuilder<OrganizationType> builder)
    {
        builder.ToTable("OrganizationType");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
