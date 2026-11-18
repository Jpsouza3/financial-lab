namespace financial_lab_transfer.Business.Models;

public sealed class UtcClock : TimeProvider
{
    public static readonly UtcClock Instance = new();

    public static DateTime Now => Truncate(DateTime.UtcNow);

    public static DateTime Truncate(DateTime value) => new(value.Ticks - value.Ticks % 10, value.Kind);

    public override DateTimeOffset GetUtcNow()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Ticks - now.Ticks % 10, TimeSpan.Zero);
    }
}
