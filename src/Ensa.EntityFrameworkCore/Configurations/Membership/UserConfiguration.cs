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

        // ---- Identity / personal details ----

        // ---- Duty / employment ----

        // ---- Medula (SGK) credentials — encrypted columns ----

        // ---- Foreign key indexes ----

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.PermissionGroupId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });

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
