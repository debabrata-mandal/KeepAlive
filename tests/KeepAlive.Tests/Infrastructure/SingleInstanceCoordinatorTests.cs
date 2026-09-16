using KeepAlive.Infrastructure;

namespace KeepAlive.Tests.Infrastructure;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void FirstCoordinatorOwnsMutexAndSecondDoesNot()
    {
        var applicationId = $"KeepAlive.Tests.{Guid.NewGuid():N}";

        using var primary = new SingleInstanceCoordinator(applicationId);
        using var secondary = new SingleInstanceCoordinator(applicationId);

        Assert.True(primary.IsPrimaryInstance);
        Assert.False(secondary.IsPrimaryInstance);
    }

    [Fact]
    public async Task SecondaryNotificationRaisesActivationRequest()
    {
        var applicationId = $"KeepAlive.Tests.{Guid.NewGuid():N}";
        var activationReceived = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var primary = new SingleInstanceCoordinator(applicationId);
        primary.ActivationRequested += (_, _) => activationReceived.TrySetResult();
        primary.StartListening();

        using var secondary = new SingleInstanceCoordinator(applicationId);
        var notified = await secondary.NotifyPrimaryAsync();

        Assert.True(notified);
        await activationReceived.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }
}
