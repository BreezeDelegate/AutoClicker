namespace AutoClicker.Core;

public sealed class SystemDelayProvider : IDelayProvider
{
    public Task DelayAsync(int milliseconds, CancellationToken cancellationToken) =>
        Task.Delay(milliseconds, cancellationToken);
}
