using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="TrainingGroup"/> table mapping.
/// <para>A HOST reference table (it does not implement <c>IMultiTenant</c>) — there is no <c>TenantId</c> index.</para>
/// </summary>
public class TrainingGroupConfiguration : IEntityTypeConfiguration<TrainingGroup>
{
    public void Configure(EntityTypeBuilder<TrainingGroup> builder)
    {
        builder.ToTable("TrainingGroup");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TrainingGroupName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.TrainingGroupCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Search by code. NOT UNIQUE: in the legacy data the group code was free text and
        // could repeat; uniqueness is enforced at the application level.
        builder.HasIndex(x => x.TrainingGroupCode);

        builder.HasIndex(x => x.OrderNo);
    }
}
