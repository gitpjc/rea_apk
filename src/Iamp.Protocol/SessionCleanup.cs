using System.Runtime.InteropServices;

namespace Iamp.Protocol;

/// <summary>Best-effort unsubscribe after disconnect; always release both Windows device resources.</summary>
public static class SessionCleanup
{
    public static async Task CloseAsync(Func<Task> unsubscribe, Action releaseService, Action releaseDevice)
    {
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(releaseService);
        ArgumentNullException.ThrowIfNull(releaseDevice);
        static void Release(Action action)
        {
            try { action(); }
            catch (Exception ex) when (ex is ObjectDisposedException or COMException) { }
        }
        try
        {
            try
            {
                var task = unsubscribe();
                _ = task.ContinueWith(static failed => _ = failed.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                await task.WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or ObjectDisposedException or COMException or OperationCanceledException) { }
        }
        finally
        {
            try { Release(releaseService); }
            finally { Release(releaseDevice); }
        }
    }
}
