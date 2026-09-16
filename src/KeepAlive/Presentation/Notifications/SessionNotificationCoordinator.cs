using KeepAlive.Core;
using KeepAlive.Core.Events;
using KeepAlive.Core.Models;

namespace KeepAlive.Presentation.Notifications;

public sealed class SessionNotificationCoordinator : IDisposable
{
    private static readonly TimeSpan WarningThreshold = TimeSpan.FromMinutes(5);

    private readonly IUserNotificationService _notifications;
    private readonly Func<bool> _notifyBeforeEnd;
    private readonly Func<bool> _notifyOnStop;
    private readonly ISessionController _sessionController;

    private Guid? _activeSessionId;
    private TimeSpan? _previousRemaining;
    private bool _warningShown;

    public SessionNotificationCoordinator(
        ISessionController sessionController,
        IUserNotificationService notifications,
        Func<bool> notifyBeforeEnd,
        Func<bool> notifyOnStop)
    {
        _sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _notifyBeforeEnd = notifyBeforeEnd ?? throw new ArgumentNullException(nameof(notifyBeforeEnd));
        _notifyOnStop = notifyOnStop ?? throw new ArgumentNullException(nameof(notifyOnStop));
        _sessionController.StateChanged += OnStateChanged;
        _sessionController.SessionCompleted += OnSessionCompleted;
    }

    public void Dispose()
    {
        _sessionController.StateChanged -= OnStateChanged;
        _sessionController.SessionCompleted -= OnSessionCompleted;
    }

    private void OnStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        if (e.Snapshot.Status != SessionStatus.Active || e.Snapshot.ActiveSession is null)
        {
            return;
        }

        if (_activeSessionId != e.Snapshot.ActiveSession.Id)
        {
            _activeSessionId = e.Snapshot.ActiveSession.Id;
            _previousRemaining = e.Snapshot.Remaining;
            _warningShown = false;
            return;
        }

        if (_notifyBeforeEnd() && !_warningShown && _previousRemaining > WarningThreshold && e.Snapshot.Remaining <= WarningThreshold)
        {
            _notifications.Show(new UserNotification(
                "Keep Alive",
                "The keep-awake session will end in 5 minutes.",
                NotificationKind.Warning));
            _warningShown = true;
        }

        _previousRemaining = e.Snapshot.Remaining;
    }

    private void OnSessionCompleted(object? sender, SessionCompletedEventArgs e)
    {
        UserNotification? notification = e.Session.EndReason switch
        {
            SessionEndReason.TimerExpired when _notifyOnStop() => new UserNotification("Keep Alive", "The keep-awake session ended on schedule.", NotificationKind.Information),
            SessionEndReason.StoppedManually when _notifyOnStop() => new UserNotification("Keep Alive", "The keep-awake session was stopped.", NotificationKind.Information),
            SessionEndReason.NativeError => new UserNotification(
                "Keep Alive error",
                e.Session.ErrorMessage ?? "Windows could not update the keep-awake request.",
                NotificationKind.Error),
            _ => null,
        };

        if (notification is not null)
        {
            _notifications.Show(notification);
        }

        _activeSessionId = null;
        _previousRemaining = null;
        _warningShown = false;
    }
}
