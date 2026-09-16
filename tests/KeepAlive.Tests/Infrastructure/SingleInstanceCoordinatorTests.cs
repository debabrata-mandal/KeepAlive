using KeepAlive.Infrastructure;

namespace KeepAlive.Tests.Infrastructure;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void FirstCoordinatorOwnsMutexAndSecondDoesNot()
    {
        string applicationId = $"KeepAlive.Tests.{Guid.NewGuid():N}";

        using SingleInstanceCoordinator primary = new(applicationId);
        using SingleInstanceCoordinator secondary = new(applicationId);

        Assert.True(primary.IsPrimaryInstance);
        Assert.False(secondary.IsPrimaryInstance);
    }

    [Fact]
    public async Task SecondaryNotificationRaisesActivationRequest()
    {
        string applicationId = $"KeepAlive.Tests.{Guid.NewGuid():N}";
        TaskCompletionSource activationReceived = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using SingleInstanceCoordinator primary = new(applicationId);
        primary.ActivationRequested += (_, _) => activationReceived.TrySetResult();
        primary.StartListening();

        using SingleInstanceCoordinator secondary = new(applicationId);
        bool notified = await secondary.NotifyPrimaryAsync();

        Assert.True(notified);
        await activationReceived.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }
}
