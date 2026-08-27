using System.Drawing;

namespace AutoClicker.Core;

public enum ClickButton
{
    Left = 0,
    Right = 1,
    Middle = 2,
    X1 = 3,
    X2 = 4
}

public enum ClickAction
{
    Single = 0,
    Double = 1
}

public sealed class ClickRunOptions
{
    public const int MinimumIntervalMilliseconds = 25;
    public const int MaximumIntervalMilliseconds = 86_400_000;

    private ClickRunOptions(
        int intervalMilliseconds,
        int varianceMilliseconds,
        ClickButton button,
        ClickAction action,
        int? repeatCount,
        Point? fixedPosition)
    {
        IntervalMilliseconds = intervalMilliseconds;
        VarianceMilliseconds = varianceMilliseconds;
        Button = button;
        Action = action;
        RepeatCount = repeatCount;
        FixedPosition = fixedPosition;
    }

    public int IntervalMilliseconds { get; }
    public int VarianceMilliseconds { get; }
    public ClickButton Button { get; }
    public ClickAction Action { get; }
    public int? RepeatCount { get; }
    public Point? FixedPosition { get; }

    public static ClickRunOptions Create(
        int intervalMilliseconds,
        int varianceMilliseconds,
        ClickButton button,
        ClickAction action,
        int? repeatCount,
        Point? fixedPosition)
    {
        if (intervalMilliseconds < MinimumIntervalMilliseconds || intervalMilliseconds > MaximumIntervalMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds), $"Interval must be between {MinimumIntervalMilliseconds} and {MaximumIntervalMilliseconds} ms.");

        if (varianceMilliseconds < 0 || varianceMilliseconds > intervalMilliseconds - MinimumIntervalMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(varianceMilliseconds), $"Variance must keep the minimum possible delay at or above {MinimumIntervalMilliseconds} ms.");

        if (repeatCount is <= 0)
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "Finite repeat count must be greater than zero.");

        return new ClickRunOptions(intervalMilliseconds, varianceMilliseconds, button, action, repeatCount, fixedPosition);
    }

    public int GetDelayMilliseconds(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return VarianceMilliseconds == 0
            ? IntervalMilliseconds
            : IntervalMilliseconds + random.Next(-VarianceMilliseconds, VarianceMilliseconds + 1);
    }
}
