namespace Iamp.Protocol;

/// <summary>Reply to the exact peer request observed during mobile connection and Tools entry.</summary>
public sealed class ToolRequestResponder : IDisposable
{
    private readonly CommandSession commands;
    private readonly object gate = new();
    private Task lastAcknowledgement = Task.CompletedTask;
    private bool disposed;

    public ToolRequestResponder(CommandSession commands)
    {
        this.commands = commands;
        commands.UnsolicitedPayloadReceived += OnRequest;
    }

    public Task LastAcknowledgement { get { lock (gate) return lastAcknowledgement; } }

    private void OnRequest(byte[] payload)
    {
        if (payload.Length != 2 || payload[0] != 0x49 || payload[1] > 1) return;
        lock (gate)
        {
            if (disposed) return;
            lastAcknowledgement = commands.WriteAsync([0x4A, 0x01], CancellationToken.None);
            _ = lastAcknowledgement.ContinueWith(task =>
            {
                var error = task.Exception!;
                lock (gate) { if (!disposed) commands.Fail(error); }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    public void Dispose()
    {
        lock (gate) disposed = true;
        commands.UnsolicitedPayloadReceived -= OnRequest;
    }
}
