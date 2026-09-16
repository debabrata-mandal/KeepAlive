using KeepAlive.Core.Models;
using KeepAlive.Presentation.Notifications;
using KeepAlive.Tests.TestDoubles;

namespace KeepAlive.Tests.Presentation;

public sealed class SessionNotificationCoordinatorTests
{
    [Fact]
    public void ShowsOneWarningWhenSessionCrossesFiveMinutesRemaining()
    {
        FakeSessionController controller = new();
        FakeUserNotificationService notifications = new();
        using SessionNotificationCoordinator coordinator = new(controller, notifications, () => true, () => true);
        SessionRecord session = CreateSession(TimeSpan.FromMinutes(15));

        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(6)));
        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(5)));
        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(4)));

        UserNotification notification = Assert.Single(notifications.Notifications);
        Assert.Equal(NotificationKind.Warning, notification.Kind);
    }

    [Fact]
    public void DoesNotShowFiveMinuteWarningForShortSession()
    {
        FakeSessionController controller = new();
        FakeUserNotificationService notifications = new();
        using SessionNotificationCoordinator coordinator = new(controller, notifications, () => true, () => true);
        SessionRecord session = CreateSession(TimeSpan.FromMinutes(3));

        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(3)));
        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(2)));

        Assert.Empty(notifications.Notifications);
    }

    [Theory]
    [InlineData(SessionEndReason.TimerExpired, NotificationKind.Information)]
    [InlineData(SessionEndReason.StoppedManually, NotificationKind.Information)]
    [InlineData(SessionEndReason.NativeError, NotificationKind.Error)]
    public void ShowsCompletionNotification(SessionEndReason reason, NotificationKind kind)
    {
        FakeSessionController controller = new();
        FakeUserNotificationService notifications = new();
        using SessionNotificationCoordinator coordinator = new(controller, notifications, () => true, () => true);
        SessionRecord session = CreateSession(TimeSpan.FromHours(1)) with { EndReason = reason, ErrorMessage = "Failure" };

        controller.Complete(session);

        UserNotification notification = Assert.Single(notifications.Notifications);
        Assert.Equal(kind, notification.Kind);
    }

    [Fact]
    public void DoesNotNotifyWhenApplicationExits()
    {
        FakeSessionController controller = new();
        FakeUserNotificationService notifications = new();
        using SessionNotificationCoordinator coordinator = new(controller, notifications, () => true, () => true);
        SessionRecord session = CreateSession(TimeSpan.FromHours(1)) with { EndReason = SessionEndReason.ApplicationExit };

        controller.Complete(session);

        Assert.Empty(notifications.Notifications);
    }

    [Fact]
    public void RespectsDisabledWarningSetting()
    {
        FakeSessionController controller = new();
        FakeUserNotificationService notifications = new();
        using SessionNotificationCoordinator coordinator = new(controller, notifications, () => false, () => true);
        SessionRecord session = CreateSession(TimeSpan.FromMinutes(15));

        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(6)));
        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, session, TimeSpan.FromMinutes(5)));

        Assert.Empty(notifications.Notifications);
    }

    [Fact]
    public void RespectsDisabledStopSetting()
    {
        FakeSessionController controller = new();
        FakeUserNotificationService notifications = new();
        using SessionNotificationCoordinator coordinator = new(controller, notifications, () => true, () => false);
        SessionRecord session = CreateSession(TimeSpan.FromHours(1)) with { EndReason = SessionEndReason.StoppedManually };

        controller.Complete(session);

        Assert.Empty(notifications.Notifications);
    }

    private static SessionRecord CreateSession(TimeSpan duration)
    {
        DateTimeOffset start = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        return new SessionRecord(Guid.NewGuid(), start, start.Add(duration), null, duration, null, null);
    }
}
