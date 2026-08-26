using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// <see cref="User"/> configuration.
/// <para>
/// <b>Scope:</b> this class configures only the fields ADDED by Ensa. The ASP.NET Core Identity fields such
/// as <c>UserName</c>, <c>Email</c>, <c>PasswordHash</c> and <c>SecurityStamp</c> — and the table name — are
/// LEFT ALONE; the table name is already set by <c>EnsaDbContext.ConfigureIdentityTables</c>.
/// </para>
/// <para>
/// <b>Identity index correction:</b> Identity creates a GLOBAL unique index (<c>UserNameIndex</c>) on
/// <c>NormalizedUserName</c>. That is WRONG in a multi-tenant model: two different organizations must be able
/// to use the same user name. The index is removed and replaced with a composite unique index on
/// <c>(TenantId, NormalizedUserName)</c>.
/// </para>
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // The table name and schema are set by EnsaDbContext.ConfigureIdentityTables.

        // Computed property — no column must be generated.
        builder.Ignore(x => x.FullName);

        // ---- Identity / personal details ----

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // The encrypter is deterministic, which is what makes WHERE and UNIQUE work on NationalId.
        builder.Property(x => x.NationalId!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.NationalId);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.Property(x => x.Gsm)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Phone);

        builder.Property(x => x.Color)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Color);

        // ---- Duty / employment ----

        builder.Property(x => x.GrossSalary).HasPrecision(18, 2);

        // ---- Medula (SGK) credentials — encrypted columns ----

        builder.Property(x => x.BranchCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.MedulaUserName!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.MedulaPassword!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Name);

        // ---- Foreign key indexes ----

        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.PhotoDocumentId);
        builder.HasIndex(x => x.OfficeId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.PermissionGroupId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });

        // Two active users cannot share a national id within the same organization.
        builder.HasIndex(x => new { x.TenantId, x.NationalId })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0 AND [NationalId] IS NOT NULL");

        // ---- Identity index correction ----

        IdentityIndexHelper.RemoveIndexOn(builder, nameof(User.NormalizedUserName));

        builder.HasIndex(x => new { x.TenantId, x.NormalizedUserName })
               .IsUnique()
               .HasDatabaseName("IX_User_TenantId_NormalizedUserName");

        // For searching by e-mail address (NOT unique — Identity does not make it unique either).
        builder.HasIndex(x => new { x.TenantId, x.NormalizedEmail })
               .HasDatabaseName("IX_User_TenantId_NormalizedEmail");
    }
}
