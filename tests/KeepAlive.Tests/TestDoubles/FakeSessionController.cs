using KeepAlive.Core;
using KeepAlive.Core.Events;
using KeepAlive.Core.Models;

namespace KeepAlive.Tests.TestDoubles;

internal sealed class FakeSessionController : ISessionController
{
    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    public event EventHandler<SessionCompletedEventArgs>? SessionCompleted;

    public SessionSnapshot Snapshot { get; private set; } = SessionSnapshot.Inactive;

    public TimeSpan? StartedDuration { get; private set; }

    public int StopCalls { get; private set; }

    public void Start(TimeSpan duration) => StartedDuration = duration;

    public SessionRecord? Stop()
    {
        StopCalls++;
        return null;
    }

    public void HandleResume()
    {
    }

    public void Shutdown()
    {
    }

    public void SetSnapshot(SessionSnapshot snapshot)
    {
        Snapshot = snapshot;
        StateChanged?.Invoke(this, new SessionStateChangedEventArgs(snapshot));
    }

    public void Complete(SessionRecord session)
    {
        Snapshot = SessionSnapshot.Inactive;
        SessionCompleted?.Invoke(this, new SessionCompletedEventArgs(session));
    }
}
