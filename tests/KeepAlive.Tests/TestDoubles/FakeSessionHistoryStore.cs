using KeepAlive.Core.Models;
using KeepAlive.History.Services;

namespace KeepAlive.Tests.TestDoubles;

internal sealed class FakeSessionHistoryStore : ISessionHistoryStore
{
    private readonly List<SessionRecord> _sessions = [];

    public IReadOnlyList<SessionRecord> Sessions => _sessions;

    public IReadOnlyList<SessionRecord> ReadAll() => _sessions.OrderByDescending(session => session.StartedAtUtc).ToArray();

    public void Upsert(SessionRecord session)
    {
        int index = _sessions.FindIndex(existing => existing.Id == session.Id);
        if (index >= 0)
        {
            _sessions[index] = session;
        }
        else
        {
            _sessions.Add(session);
        }
    }

    public void RecoverInterrupted(DateTimeOffset recoveredAtUtc)
    {
        for (int index = 0; index < _sessions.Count; index++)
        {
            SessionRecord session = _sessions[index];
            if (session.EndReason is null)
            {
                _sessions[index] = session with
                {
                    EndedAtUtc = recoveredAtUtc,
                    EndReason = SessionEndReason.Interrupted,
                };
            }
        }
    }

    public void Clear() => _sessions.Clear();
}
