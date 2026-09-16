using KeepAlive.Core.Models;
using KeepAlive.History.Services;
using KeepAlive.Tests.TestDoubles;

namespace KeepAlive.Tests.History;

public sealed class JsonSessionHistoryStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpsertReplacesExistingSession()
    {
        string path = CreateTemporaryPath();

        try
        {
            JsonSessionHistoryStore store = new(path, new FakeClock(Now));
            SessionRecord active = CreateSession();
            store.Upsert(active);

            store.Upsert(active with { EndedAtUtc = Now.AddMinutes(10), EndReason = SessionEndReason.StoppedManually });

            SessionRecord saved = Assert.Single(store.ReadAll());
            Assert.Equal(SessionEndReason.StoppedManually, saved.EndReason);
        }
        finally
        {
            DeleteIfPresent(path);
        }
    }

    [Fact]
    public void RecoveryMarksUnfinishedSessionInterrupted()
    {
        string path = CreateTemporaryPath();

        try
        {
            JsonSessionHistoryStore store = new(path, new FakeClock(Now));
            store.Upsert(CreateSession());

            store.RecoverInterrupted(Now.AddMinutes(5));

            SessionRecord saved = Assert.Single(store.ReadAll());
            Assert.Equal(SessionEndReason.Interrupted, saved.EndReason);
            Assert.Equal(Now.AddMinutes(5), saved.EndedAtUtc);
        }
        finally
        {
            DeleteIfPresent(path);
        }
    }

    [Fact]
    public void ClearRemovesAllSessions()
    {
        string path = CreateTemporaryPath();

        try
        {
            JsonSessionHistoryStore store = new(path, new FakeClock(Now));
            store.Upsert(CreateSession());

            store.Clear();

            Assert.Empty(store.ReadAll());
        }
        finally
        {
            DeleteIfPresent(path);
        }
    }

    private static SessionRecord CreateSession()
    {
        return new SessionRecord(Guid.NewGuid(), Now, Now.AddHours(1), null, TimeSpan.FromHours(1), null, null);
    }

    private static string CreateTemporaryPath() => Path.Combine(Path.GetTempPath(), $"keep-alive-{Guid.NewGuid():N}.json");

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
