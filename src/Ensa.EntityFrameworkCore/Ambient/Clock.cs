using Ensa.Domain.Common;

namespace Ensa.EntityFrameworkCore.Ambient;

/// <summary>
/// Default <see cref="IClock"/> implementation.
/// <para>
/// <see cref="Now"/> returns the <b>local server time</b>. This behaviour is preserved because the legacy Ensa
/// application kept every audit field in Turkish local time. Where UTC is required,
/// <see cref="UtcNow"/> must be used.
/// </para>
/// <para>
/// In tests this class is replaced by a fake <see cref="IClock"/> returning a fixed time, which removes the
/// dependency on the wall clock.
/// </para>
/// </summary>
public sealed class Clock : IClock
{
    /// <summary>Process-wide shared instance (for design-time / seed scenarios).</summary>
    public static readonly Clock Instance = new();

    /// <inheritdoc />
    public DateTime Now => DateTime.Now;

    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
