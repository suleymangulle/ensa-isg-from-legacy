using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

public class UserEmploymentConfiguration : IEntityTypeConfiguration<UserEmployment>
{
    public void Configure(EntityTypeBuilder<UserEmployment> builder)
    {
        builder.ToTable("UserEmployment");
        builder.HasKey(x => x.Id);

        // One current employment per account. A past one is a StaffCostBaseline row.
        builder.HasIndex(x => x.UserId).IsUnique();

        // Money is decimal, never float. The legacy column was a float.
        builder.Property(x => x.GrossSalary)
               .HasPrecision(18, 2);

        builder.HasIndex(x => x.UserTypeId);
    }
}
