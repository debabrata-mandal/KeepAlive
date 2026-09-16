using KeepAlive.Core.Models;

namespace KeepAlive.History.Services;

public interface ISessionHistoryStore
{
    IReadOnlyList<SessionRecord> ReadAll();

    void Upsert(SessionRecord session);

    void RecoverInterrupted(DateTimeOffset recoveredAtUtc);

    void Clear();
}
