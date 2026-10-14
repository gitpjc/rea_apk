using System.Runtime.InteropServices;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class SessionCleanupTests
{
    [Fact]
    public async Task CancelledNativeUnsubscribeStillReleasesBothHandles()
    {
        var released = new List<string>();
        await SessionCleanup.CloseAsync(() => Task.FromCanceled(new CancellationToken(true)),
            () => released.Add("service"), () => released.Add("device"));
        Assert.Equal(new[] { "service", "device" }, released);
    }

    [Fact]
    public async Task AlreadyDisposedNotificationStillReleasesBothHandles()
    {
        var released = new List<string>();
        await SessionCleanup.CloseAsync(() => throw new ObjectDisposedException("notification"),
            () => released.Add("service"), () => released.Add("device"));
        Assert.Equal(new[] { "service", "device" }, released);
    }

    [Fact]
    public async Task AlreadyClosedServiceAndDeviceAreExpectedAfterDisconnect()
    {
        var released = new List<string>();
        await SessionCleanup.CloseAsync(() => Task.FromException(new IOException("Disconnected")),
            () => { released.Add("service"); throw new ObjectDisposedException("service"); },
            () => { released.Add("device"); throw new COMException("Native handle closed"); });
        Assert.Equal(new[] { "service", "device" }, released);
    }

    [Fact]
    public async Task UnexpectedServiceFailureDoesNotSkipDeviceRelease()
    {
        var released = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => SessionCleanup.CloseAsync(() => Task.CompletedTask,
            () => throw new InvalidOperationException("Unexpected"), () => released = true));
        Assert.True(released);
    }

    [Fact]
    public async Task UnexpectedUnsubscribeFailureStillReleasesBothHandlesAndPropagates()
    {
        var released = new List<string>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => SessionCleanup.CloseAsync(
            () => Task.FromException(new InvalidOperationException("Unexpected")),
            () => released.Add("service"), () => released.Add("device")));
        Assert.Equal(new[] { "service", "device" }, released);
    }
}
