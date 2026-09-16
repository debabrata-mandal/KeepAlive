using KeepAlive.Core.Models;
using KeepAlive.History.Services;
using KeepAlive.Tests.TestDoubles;

namespace KeepAlive.Tests.History;

public sealed class SessionHistoryCoordinatorTests
{
    [Fact]
    public void ActiveSessionIsSavedOnceAndUpdatedOnCompletion()
    {
        DateTimeOffset now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        FakeSessionController controller = new();
        FakeSessionHistoryStore store = new();
        FakeClock clock = new(now);
        using SessionHistoryCoordinator coordinator = new(store, controller, clock);
        SessionRecord active = new(Guid.NewGuid(), now, now.AddHours(1), null, TimeSpan.FromHours(1), null, null);

        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, active, TimeSpan.FromHours(1)));
        controller.SetSnapshot(new SessionSnapshot(SessionStatus.Active, active, TimeSpan.FromMinutes(59)));
        controller.Complete(active with { EndedAtUtc = now.AddMinutes(10), EndReason = SessionEndReason.StoppedManually });

        SessionRecord saved = Assert.Single(store.Sessions);
        Assert.Equal(SessionEndReason.StoppedManually, saved.EndReason);
    }

    [Fact]
    public void ClearRaisesHistoryChanged()
    {
        DateTimeOffset now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        FakeSessionController controller = new();
        FakeSessionHistoryStore store = new();
        using SessionHistoryCoordinator coordinator = new(store, controller, new FakeClock(now));
        int changeCount = 0;
        coordinator.HistoryChanged += (_, _) => changeCount++;

        coordinator.Clear();

        Assert.Equal(1, changeCount);
        Assert.Empty(store.Sessions);
    }
}
