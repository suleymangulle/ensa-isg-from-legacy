using Ensa.Domain.Common;

namespace Ensa.Domain.Tests;

/// <summary>
/// <see cref="IClock"/> pinned to a fixed instant, so date-dependent rules stay deterministic.
/// <para>
/// Domain services take <see cref="IClock"/> rather than reading <c>DateTime.Now</c> precisely so
/// that a test can decide what "now" is; without it, a rule that compares against today would pass
/// or fail depending on the day it runs.
/// </para>
/// </summary>
public sealed class FixedClock(DateTime now) : IClock
{
    public DateTime Now { get; } = now;

    public DateTime UtcNow => Now.ToUniversalTime();

    public DateOnly Today => DateOnly.FromDateTime(Now);
}
