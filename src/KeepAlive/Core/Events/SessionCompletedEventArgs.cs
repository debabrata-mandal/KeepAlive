using KeepAlive.Core.Models;

namespace KeepAlive.Core.Events;

public sealed class SessionCompletedEventArgs(SessionRecord session) : EventArgs
{
    public SessionRecord Session { get; } = session;
}
