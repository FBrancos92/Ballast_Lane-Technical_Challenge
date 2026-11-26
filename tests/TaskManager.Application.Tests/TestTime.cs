namespace TaskManager.Application.Tests;

/// <summary>A clock frozen at a known instant so date rules are deterministic.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider()
        : this(DefaultNow)
    {
    }

    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);
}
