namespace RoomBook.Unit.Tests.Testing;

/// <summary>
/// Deterministic <see cref="TimeProvider"/> so tests control "now".
/// </summary>
internal sealed class FixedTimeProvider(DateTime utcNow)
    : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new DateTimeOffset(utcNow);
}
