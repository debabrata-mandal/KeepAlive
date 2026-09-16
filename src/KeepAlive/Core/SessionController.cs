using KeepAlive.Core.Events;
using KeepAlive.Core.Models;

namespace KeepAlive.Core;

public sealed class SessionController : ISessionController, IDisposable
{
    private readonly IClock _clock;
    private readonly IKeepAwakeService _keepAwakeService;
    private readonly int _owningThreadId;
    private readonly ISessionTimer _timer;

    private SessionRecord? _activeSession;
    private bool _disposed;
    private SessionSnapshot _snapshot = SessionSnapshot.Inactive;

    public SessionController(IKeepAwakeService keepAwakeService, IClock clock, ISessionTimer timer)
    {
        _keepAwakeService = keepAwakeService ?? throw new ArgumentNullException(nameof(keepAwakeService));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        _owningThreadId = Environment.CurrentManagedThreadId;
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    public event EventHandler<SessionCompletedEventArgs>? SessionCompleted;

    public SessionSnapshot Snapshot => _snapshot;

    public void Start(TimeSpan duration)
    {
        VerifyReady();
        SessionDurationPolicy.Validate(duration);

        if (_activeSession is not null)
        {
            throw new InvalidOperationException("A keep-awake session is already active.");
        }

        var startedAtUtc = _clock.UtcNow;
        var pendingSession = new SessionRecord(
            Guid.NewGuid(),
            startedAtUtc,
            startedAtUtc.Add(duration),
            EndedAtUtc: null,
            duration,
            EndReason: null,
            ErrorMessage: null);

        try
        {
            _keepAwakeService.Start();
        }
        catch (KeepAwakeException exception)
        {
            var failedSession = pendingSession with
            {
                EndedAtUtc = _clock.UtcNow,
                EndReason = SessionEndReason.NativeError,
                ErrorMessage = exception.Message,
            };

            RaiseStateChanged();
            SessionCompleted?.Invoke(this, new SessionCompletedEventArgs(failedSession));
            throw;
        }

        _activeSession = pendingSession;
        _snapshot = new SessionSnapshot(SessionStatus.Active, pendingSession, duration);
        _timer.Start();
        RaiseStateChanged();
    }

    public SessionRecord? Stop()
    {
        VerifyReady();
        return CompleteSession(SessionEndReason.StoppedManually);
    }

    public void HandleResume()
    {
        VerifyReady();
        RefreshFromClock();
    }

    public void Shutdown()
    {
        VerifyReady();
        CompleteSession(SessionEndReason.ApplicationExit);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        VerifyOwningThread();

        if (_activeSession is not null)
        {
            try
            {
                CompleteSession(SessionEndReason.ApplicationExit);
            }
            catch (KeepAwakeException)
            {
                // The process exit will clear any remaining execution-state request.
            }
        }

        _timer.Tick -= OnTimerTick;
        _timer.Dispose();
        _disposed = true;
    }

    private SessionRecord? CompleteSession(SessionEndReason reason)
    {
        if (_activeSession is null)
        {
            return null;
        }

        _timer.Stop();

        KeepAwakeException? stopFailure = null;
        try
        {
            _keepAwakeService.Stop();
        }
        catch (KeepAwakeException exception)
        {
            stopFailure = exception;
        }

        var completedSession = _activeSession with
        {
            EndedAtUtc = _clock.UtcNow,
            EndReason = stopFailure is null ? reason : SessionEndReason.NativeError,
            ErrorMessage = stopFailure?.Message,
        };

        _activeSession = null;
        _snapshot = SessionSnapshot.Inactive;
        RaiseStateChanged();
        SessionCompleted?.Invoke(this, new SessionCompletedEventArgs(completedSession));

        if (stopFailure is not null)
        {
            throw stopFailure;
        }

        return completedSession;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        VerifyOwningThread();

        try
        {
            RefreshFromClock();
        }
        catch (KeepAwakeException)
        {
            // Completion has already transitioned to inactive and surfaced NativeError.
        }
    }

    private void RefreshFromClock()
    {
        if (_activeSession is null)
        {
            return;
        }

        var remaining = _activeSession.PlannedEndUtc - _clock.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            CompleteSession(SessionEndReason.TimerExpired);
            return;
        }

        _snapshot = new SessionSnapshot(SessionStatus.Active, _activeSession, remaining);
        RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new SessionStateChangedEventArgs(_snapshot));
    }

    private void VerifyReady()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        VerifyOwningThread();
    }

    private void VerifyOwningThread()
    {
        if (Environment.CurrentManagedThreadId != _owningThreadId)
        {
            throw new InvalidOperationException("Session operations must run on the thread that created the controller.");
        }
    }
}
