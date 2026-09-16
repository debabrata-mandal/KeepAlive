using KeepAlive.Core.Models;

namespace KeepAlive.Core.Events;

public sealed class SessionStateChangedEventArgs(SessionSnapshot snapshot) : EventArgs
{
    public SessionSnapshot Snapshot { get; } = snapshot;
}
