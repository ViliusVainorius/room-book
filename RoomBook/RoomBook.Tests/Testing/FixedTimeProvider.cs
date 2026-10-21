namespace RoomBook.Unit.Tests.Testing;

internal sealed class FixedTimeProvider(DateTime utcNow)
    : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new DateTimeOffset(utcNow);
}
