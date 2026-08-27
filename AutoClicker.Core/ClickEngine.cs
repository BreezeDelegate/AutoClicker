using AutoClicker.Core.Input;

namespace AutoClicker.Core;

public sealed class ClickEngine
{
    private readonly object _sync = new();
    private readonly IMouseInput _mouseInput;
    private readonly IDelayProvider _delayProvider;
    private readonly Random _random;
    private CancellationTokenSource? _cancellation;
    private Task? _runTask;
    private Task _completion = Task.CompletedTask;

    public ClickEngine(IMouseInput mouseInput, IDelayProvider delayProvider, Random? random = null)
    {
        _mouseInput = mouseInput ?? throw new ArgumentNullException(nameof(mouseInput));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
        _random = random ?? new Random();
    }

    public event EventHandler<Exception>? Faulted;
    public event EventHandler? Stopped;

    public bool IsRunning
    {
        get
        {
            lock (_sync)
                return _runTask is not null;
        }
    }

    public Task Completion
    {
        get
        {
            lock (_sync)
                return _completion;
        }
    }

    public Task<bool> StartAsync(ClickRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (_sync)
        {
            if (_runTask is not null)
                return Task.FromResult(false);

            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            var runTask = RunAsync(options, cancellation);
            _runTask = runTask;
            _completion = runTask;
            return Task.FromResult(true);
        }
    }

    public async Task StopAsync()
    {
        Task? runTask;
        CancellationTokenSource? cancellation;

        lock (_sync)
        {
            runTask = _runTask;
            cancellation = _cancellation;
        }

        if (runTask is null || cancellation is null)
            return;

        cancellation.Cancel();
        await runTask.ConfigureAwait(false);
    }

    private async Task RunAsync(ClickRunOptions options, CancellationTokenSource cancellation)
    {
        Exception? fault = null;
        var token = cancellation.Token;
        var completed = 0;

        // Ensure StartAsync publishes the task/state before this run can complete.
        await Task.Yield();

        try
        {
            while (!token.IsCancellationRequested && (options.RepeatCount is null || completed < options.RepeatCount.Value))
            {
                _mouseInput.Click(options.Button, options.Action, options.FixedPosition);
                completed++;

                if (options.RepeatCount is not null && completed >= options.RepeatCount.Value)
                    break;

                await _delayProvider.DelayAsync(options.GetDelayMilliseconds(_random), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            fault = ex;
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_cancellation, cancellation))
                {
                    _cancellation = null;
                    _runTask = null;
                }
            }
        }

        if (fault is not null)
            Faulted?.Invoke(this, fault);

        Stopped?.Invoke(this, EventArgs.Empty);
    }
}
