using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// User–office assignment (many-to-many link table). A tenant-owned record;
/// uniqueness is composite with <c>TenantId</c>.
/// </summary>
public class UserOfficeConfiguration : IEntityTypeConfiguration<UserOffice>
{
    public void Configure(EntityTypeBuilder<UserOffice> builder)
    {
        builder.ToTable("UserOffice");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.OfficeId);

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.OfficeId }).IsUnique();
    }
}
