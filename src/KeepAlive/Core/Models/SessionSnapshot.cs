namespace KeepAlive.Core.Models;

public sealed record SessionSnapshot(SessionStatus Status, SessionRecord? ActiveSession, TimeSpan Remaining)
{
    public static SessionSnapshot Inactive { get; } = new(SessionStatus.Inactive, null, TimeSpan.Zero);
}
