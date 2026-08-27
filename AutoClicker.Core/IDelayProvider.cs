namespace AutoClicker.Core;

public interface IDelayProvider
{
    Task DelayAsync(int milliseconds, CancellationToken cancellationToken);
}
