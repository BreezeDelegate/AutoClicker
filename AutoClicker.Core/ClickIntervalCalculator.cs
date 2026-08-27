namespace AutoClicker.Core;

public static class ClickIntervalCalculator
{
    public static int ToMilliseconds(int hours, int minutes, int seconds, int milliseconds)
    {
        if (hours < 0 || minutes < 0 || seconds < 0 || milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(hours), "Interval components cannot be negative.");

        var total = ((long)hours * 60 * 60 * 1000)
                    + ((long)minutes * 60 * 1000)
                    + ((long)seconds * 1000)
                    + milliseconds;

        if (total > ClickRunOptions.MaximumIntervalMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(hours), "Interval cannot exceed one day.");

        return checked((int)total);
    }
}
