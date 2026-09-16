using KeepAlive.Core.Events;
using KeepAlive.Core.Models;

namespace KeepAlive.Core;

public interface ISessionController
{
    event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    event EventHandler<SessionCompletedEventArgs>? SessionCompleted;

    SessionSnapshot Snapshot { get; }

    void Start(TimeSpan duration);

    SessionRecord? Stop();

    void HandleResume();

    void Shutdown();
}
