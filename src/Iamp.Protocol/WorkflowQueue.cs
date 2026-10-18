using System.Runtime.ExceptionServices;

namespace Iamp.Protocol;

/// <summary>Serializes complete non-reentrant workflows; active failure is terminal, queued cancellation is isolated.</summary>
public sealed class WorkflowQueue : IDisposable
{
    private readonly SemaphoreSlim queue = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private readonly object gate = new();
    private Exception? failure;
    private bool disposed;

    public bool IsAvailable { get { lock (gate) return !disposed && failure is null; } }

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(action);
        CancellationTokenSource linked;
        lock (gate)
        {
            ThrowIfUnavailable();
            linked = CancellationTokenSource.CreateLinkedTokenSource(ct, lifetime.Token);
        }
        using (linked)
        {
            try { await queue.WaitAsync(linked.Token); }
            catch (OperationCanceledException)
            {
                lock (gate) ThrowIfUnavailable();
                throw;
            }
            try
            {
                lock (gate) ThrowIfUnavailable();
                linked.Token.ThrowIfCancellationRequested();
                // Do not abandon an uncooperative action and release the queue while it still executes.
                var result = await action(linked.Token);
                lock (gate) ThrowIfUnavailable();
                linked.Token.ThrowIfCancellationRequested();
                return result;
            }
            catch (Exception ex)
            {
                lock (gate)
                {
                    failure ??= ex;
                    ThrowIfUnavailable();
                }
                throw;
            }
            finally { queue.Release(); }
        }
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
        }
    }
}
