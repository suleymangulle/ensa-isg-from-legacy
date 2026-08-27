using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

public class UserMedulaCredentialConfiguration : IEntityTypeConfiguration<UserMedulaCredential>
{
    public void Configure(EntityTypeBuilder<UserMedulaCredential> builder)
    {
        builder.ToTable("UserMedulaCredential");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.MedulaUserName!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Name);

        // Encrypted rather than hashed: it has to be sent to MEDULA, so it must be recoverable.
        builder.Property(x => x.MedulaPassword!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.BranchCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);
    }
}
