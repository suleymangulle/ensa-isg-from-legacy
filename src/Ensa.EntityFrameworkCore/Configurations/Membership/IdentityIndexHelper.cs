using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Membership;

/// <summary>
/// Helper used to remove the indexes that ASP.NET Core Identity creates by convention.
/// <para>
/// <b>Why is it needed?</b> Identity creates a GLOBAL unique index on the <c>NormalizedUserName</c> and
/// <c>NormalizedName</c> columns. Since Ensa is multi-tenant those indexes are wrong: two different
/// organizations must be able to use the same user name / role name. Because the Fluent API offers no way to
/// "remove" an index, the work is done through the model metadata. This is safe, because
/// <c>ApplyConfigurationsFromAssembly</c> runs AFTER the <c>base.OnModelCreating</c> call (see
/// <c>EnsaDbContext</c>).
/// </para>
/// </summary>
internal static class IdentityIndexHelper
{
    /// <summary>
    /// Removes the unnamed index defined on the given property (if any) from the model.
    /// Does nothing silently when the property or the index cannot be found — so that the build does not
    /// break if a future Identity release drops the index.
    /// </summary>
    internal static void RemoveIndexOn(EntityTypeBuilder builder, string propertyName)
    {
        var property = builder.Metadata.FindProperty(propertyName);
        if (property is null)
        {
            return;
        }

        var index = builder.Metadata.FindIndex(property);
        if (index is null)
        {
            return;
        }

        builder.Metadata.RemoveIndex(index);
    }
}
