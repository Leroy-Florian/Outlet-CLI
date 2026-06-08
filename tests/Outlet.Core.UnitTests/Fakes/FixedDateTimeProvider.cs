using Outlet.Kernel.Shared;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>
/// Hand-written <see cref="ICurrentDateTimeProvider"/> frozen at a fixed instant,
/// so time-dependent logic (throttle windows) is deterministic in tests.
/// </summary>
public sealed class FixedDateTimeProvider(DateTime utcNow) : ICurrentDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;

    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
