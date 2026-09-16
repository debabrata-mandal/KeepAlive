using KeepAlive.Core;
using KeepAlive.Core.Models;
using KeepAlive.Infrastructure.Persistence;

namespace KeepAlive.History.Services;

public sealed class JsonSessionHistoryStore(string path, IClock clock) : ISessionHistoryStore
{
    private const int MaximumRecords = 1_000;
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    private readonly AtomicJsonFile _file = new(path);

    public IReadOnlyList<SessionRecord> ReadAll()
    {
        List<SessionRecord> sessions = _file.Read<List<SessionRecord>>() ?? [];
        return sessions.OrderByDescending(session => session.StartedAtUtc).ToArray();
    }

    public void Upsert(SessionRecord session)
    {
        ArgumentNullException.ThrowIfNull(session);

        List<SessionRecord> sessions = _file.Read<List<SessionRecord>>() ?? [];
        int existingIndex = sessions.FindIndex(existing => existing.Id == session.Id);

        if (existingIndex >= 0)
        {
            sessions[existingIndex] = session;
        }
        else
        {
            sessions.Add(session);
        }

        DateTimeOffset cutoff = clock.UtcNow.Subtract(RetentionPeriod);
        List<SessionRecord> retained = sessions
            .Where(existing => (existing.EndedAtUtc ?? existing.StartedAtUtc) >= cutoff)
            .OrderByDescending(existing => existing.StartedAtUtc)
            .Take(MaximumRecords)
            .ToList();
        _file.Write(retained);
    }

    public void RecoverInterrupted(DateTimeOffset recoveredAtUtc)
    {
        List<SessionRecord> sessions = _file.Read<List<SessionRecord>>() ?? [];
        bool changed = false;

        for (int index = 0; index < sessions.Count; index++)
        {
            SessionRecord session = sessions[index];
            if (session.EndReason is null)
            {
                sessions[index] = session with
                {
                    EndedAtUtc = recoveredAtUtc,
                    EndReason = SessionEndReason.Interrupted,
                };
                changed = true;
            }
        }

        if (changed)
        {
            _file.Write(sessions);
        }
    }

    public void Clear() => _file.Write(Array.Empty<SessionRecord>());
}
