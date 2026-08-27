using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

public class RoleProfileConfiguration : IEntityTypeConfiguration<RoleProfile>
{
    public void Configure(EntityTypeBuilder<RoleProfile> builder)
    {
        builder.ToTable("RoleProfile");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.RoleId).IsUnique();

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);
    }
}
