using KeepAlive.Presentation.Notifications;

namespace KeepAlive.Tests.TestDoubles;

internal sealed class FakeUserNotificationService : IUserNotificationService
{
    public List<UserNotification> Notifications { get; } = [];

    public void Show(UserNotification notification) => Notifications.Add(notification);
}
