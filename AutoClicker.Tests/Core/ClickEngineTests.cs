using AutoClicker.Core;
using AutoClicker.Core.Input;
using Xunit;

namespace AutoClicker.Tests.Core;

public sealed class ClickEngineTests
{
    [Fact]
    public async Task SecondStartIsRejectedWhileRunning()
    {
        var engine = new ClickEngine(new CountingMouseInput(), new BlockingDelayProvider(), new Random(1));

        Assert.True(await engine.StartAsync(InfiniteOptions()));
        Assert.False(await engine.StartAsync(InfiniteOptions()));

        await engine.StopAsync();
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task StopIsIdempotent()
    {
        var engine = new ClickEngine(new CountingMouseInput(), new BlockingDelayProvider(), new Random(1));
        await engine.StartAsync(InfiniteOptions());

        await engine.StopAsync();
        await engine.StopAsync();

        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task FiniteRunDispatchesExactlyConfiguredRepeats()
    {
        var input = new CountingMouseInput();
        var engine = new ClickEngine(input, new ImmediateDelayProvider(), new Random(1));
        var options = ClickRunOptions.Create(25, 0, ClickButton.Left, ClickAction.Single, 3, null);

        Assert.True(await engine.StartAsync(options));
        await engine.Completion;

        Assert.Equal(3, input.ClickCount);
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task InfiniteRunStopsPromptlyWhenCancelled()
    {
        var input = new CountingMouseInput();
        var engine = new ClickEngine(input, new BlockingDelayProvider(), new Random(1));

        await engine.StartAsync(InfiniteOptions());
        await WaitUntilAsync(() => input.ClickCount > 0);
        await engine.StopAsync();

        Assert.False(engine.IsRunning);
        Assert.True(input.ClickCount >= 1);
    }

    [Fact]
    public async Task InputFailureRaisesFaultAndRestoresStoppedState()
    {
        var expected = new InvalidOperationException("dispatch failed");
        var engine = new ClickEngine(new ThrowingMouseInput(expected), new ImmediateDelayProvider(), new Random(1));
        Exception? observed = null;
        var stoppedCount = 0;
        engine.Faulted += (_, error) => observed = error;
        engine.Stopped += (_, _) => Interlocked.Increment(ref stoppedCount);

        await engine.StartAsync(ClickRunOptions.Create(25, 0, ClickButton.Left, ClickAction.Single, 1, null));
        await engine.Completion;

        Assert.Same(expected, observed);
        Assert.Equal(1, stoppedCount);
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task CompletedRunCanBeStartedAgain()
    {
        var input = new CountingMouseInput();
        var engine = new ClickEngine(input, new ImmediateDelayProvider(), new Random(1));
        var oneClick = ClickRunOptions.Create(25, 0, ClickButton.Left, ClickAction.Single, 1, null);

        Assert.True(await engine.StartAsync(oneClick));
        await engine.Completion;
        Assert.True(await engine.StartAsync(oneClick));
        await engine.Completion;

        Assert.Equal(2, input.ClickCount);
    }

    private static ClickRunOptions InfiniteOptions() =>
        ClickRunOptions.Create(25, 0, ClickButton.Left, ClickAction.Single, null, null);

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!predicate())
            await Task.Delay(5, cts.Token);
    }

    private sealed class CountingMouseInput : IMouseInput
    {
        private int _clickCount;
        public int ClickCount => Volatile.Read(ref _clickCount);
        public void Click(ClickButton button, ClickAction action, System.Drawing.Point? fixedPosition) =>
            Interlocked.Increment(ref _clickCount);
    }

    private sealed class ThrowingMouseInput(Exception error) : IMouseInput
    {
        public void Click(ClickButton button, ClickAction action, System.Drawing.Point? fixedPosition) => throw error;
    }

    private sealed class BlockingDelayProvider : IDelayProvider
    {
        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private sealed class ImmediateDelayProvider : IDelayProvider
    {
        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
