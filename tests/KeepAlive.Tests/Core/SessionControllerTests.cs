using KeepAlive.Core;
using KeepAlive.Core.Models;

namespace KeepAlive.Tests.Core;

public sealed class SessionControllerTests
{
    [Fact]
    public void StartActivatesKeepAwakeAndCreatesDeadline()
    {
        using SessionFixture fixture = new();

        fixture.Controller.Start(TimeSpan.FromHours(2), simulateInputActivity: false);

        Assert.True(fixture.KeepAwake.IsActive);
        Assert.True(fixture.Timer.IsRunning);
        Assert.Equal(SessionStatus.Active, fixture.Controller.Snapshot.Status);
        Assert.Equal(fixture.Clock.UtcNow.AddHours(2), fixture.Controller.Snapshot.ActiveSession?.PlannedEndUtc);
        Assert.Equal(TimeSpan.FromHours(2), fixture.Controller.Snapshot.Remaining);
    }

    [Fact]
    public void StartDoesNotActivateActivitySimulatorByDefault()
    {
        using SessionFixture fixture = new();

        fixture.Controller.Start(TimeSpan.FromHours(2), simulateInputActivity: false);

        Assert.False(fixture.ActivitySimulator.IsActive);
    }

    [Fact]
    public void StartActivatesActivitySimulatorWhenRequested()
    {
        using SessionFixture fixture = new();

        fixture.Controller.Start(TimeSpan.FromHours(2), simulateInputActivity: true);

        Assert.True(fixture.ActivitySimulator.IsActive);
        Assert.Equal(1, fixture.ActivitySimulator.StartCalls);
    }

    [Fact]
    public void StopDeactivatesActivitySimulator()
    {
        using SessionFixture fixture = new();
        fixture.Controller.Start(TimeSpan.FromHours(1), simulateInputActivity: true);

        fixture.Controller.Stop();

        Assert.False(fixture.ActivitySimulator.IsActive);
    }

    [Fact]
    public void StartRejectsSecondActiveSession()
    {
        using SessionFixture fixture = new();
        fixture.Controller.Start(TimeSpan.FromMinutes(30), simulateInputActivity: false);

        Assert.Throws<InvalidOperationException>(() => fixture.Controller.Start(TimeSpan.FromHours(1), simulateInputActivity: false));
        Assert.Equal(1, fixture.KeepAwake.StartCalls);
    }

    [Fact]
    public void TimerTickCalculatesRemainingTimeFromUtcDeadline()
    {
        using SessionFixture fixture = new();
        fixture.Controller.Start(TimeSpan.FromHours(2), simulateInputActivity: false);

        fixture.Clock.Advance(TimeSpan.FromMinutes(37));
        fixture.Timer.Fire();

        Assert.Equal(TimeSpan.FromMinutes(83), fixture.Controller.Snapshot.Remaining);
    }

    [Fact]
    public void TimerExpiresSessionAtDeadline()
    {
        using SessionFixture fixture = new();
        SessionRecord? completed = null;
        fixture.Controller.SessionCompleted += (_, e) => completed = e.Session;
        fixture.Controller.Start(TimeSpan.FromMinutes(15), simulateInputActivity: true);

        fixture.Clock.Advance(TimeSpan.FromMinutes(15));
        fixture.Timer.Fire();

        Assert.Equal(SessionStatus.Inactive, fixture.Controller.Snapshot.Status);
        Assert.False(fixture.KeepAwake.IsActive);
        Assert.False(fixture.ActivitySimulator.IsActive);
        Assert.False(fixture.Timer.IsRunning);
        Assert.Equal(SessionEndReason.TimerExpired, completed?.EndReason);
        Assert.Equal(fixture.Clock.UtcNow, completed?.EndedAtUtc);
    }

    [Fact]
    public void StopCompletesSessionManually()
    {
        using SessionFixture fixture = new();
        fixture.Controller.Start(TimeSpan.FromHours(1), simulateInputActivity: false);
        fixture.Clock.Advance(TimeSpan.FromMinutes(10));

        SessionRecord? completed = fixture.Controller.Stop();

        Assert.NotNull(completed);
        Assert.Equal(SessionEndReason.StoppedManually, completed.EndReason);
        Assert.Equal(fixture.Clock.UtcNow, completed.EndedAtUtc);
        Assert.Equal(SessionStatus.Inactive, fixture.Controller.Snapshot.Status);
        Assert.False(fixture.KeepAwake.IsActive);
    }

    [Fact]
    public void StopWhenInactiveDoesNothing()
    {
        using SessionFixture fixture = new();

        SessionRecord? completed = fixture.Controller.Stop();

        Assert.Null(completed);
        Assert.Equal(0, fixture.KeepAwake.StopCalls);
    }

    [Fact]
    public void ResumeBeforeDeadlineKeepsSessionActive()
    {
        using SessionFixture fixture = new();
        fixture.Controller.Start(TimeSpan.FromHours(1), simulateInputActivity: false);
        fixture.Clock.Advance(TimeSpan.FromMinutes(20));

        fixture.Controller.HandleResume();

        Assert.Equal(SessionStatus.Active, fixture.Controller.Snapshot.Status);
        Assert.Equal(TimeSpan.FromMinutes(40), fixture.Controller.Snapshot.Remaining);
        Assert.True(fixture.KeepAwake.IsActive);
    }

    [Fact]
    public void ResumeAfterDeadlineExpiresSession()
    {
        using SessionFixture fixture = new();
        SessionRecord? completed = null;
        fixture.Controller.SessionCompleted += (_, e) => completed = e.Session;
        fixture.Controller.Start(TimeSpan.FromMinutes(30), simulateInputActivity: false);
        fixture.Clock.Advance(TimeSpan.FromMinutes(31));

        fixture.Controller.HandleResume();

        Assert.Equal(SessionStatus.Inactive, fixture.Controller.Snapshot.Status);
        Assert.Equal(SessionEndReason.TimerExpired, completed?.EndReason);
        Assert.False(fixture.KeepAwake.IsActive);
    }

    [Fact]
    public void NativeStartFailureLeavesControllerInactiveAndRecordsError()
    {
        using SessionFixture fixture = new();
        SessionRecord? completed = null;
        fixture.KeepAwake.ThrowOnStart = true;
        fixture.Controller.SessionCompleted += (_, e) => completed = e.Session;

        KeepAwakeException exception = Assert.Throws<KeepAwakeException>(
            () => fixture.Controller.Start(TimeSpan.FromHours(1), simulateInputActivity: true));

        Assert.Equal("Start failed.", exception.Message);
        Assert.Equal(SessionStatus.Inactive, fixture.Controller.Snapshot.Status);
        Assert.False(fixture.Timer.IsRunning);
        Assert.False(fixture.ActivitySimulator.IsActive);
        Assert.Equal(SessionEndReason.NativeError, completed?.EndReason);
        Assert.Equal("Start failed.", completed?.ErrorMessage);
    }

    [Fact]
    public void NativeStopFailureStillTransitionsControllerToInactive()
    {
        using SessionFixture fixture = new();
        SessionRecord? completed = null;
        fixture.Controller.SessionCompleted += (_, e) => completed = e.Session;
        fixture.Controller.Start(TimeSpan.FromHours(1), simulateInputActivity: false);
        fixture.KeepAwake.ThrowOnStop = true;

        KeepAwakeException exception = Assert.Throws<KeepAwakeException>(() => fixture.Controller.Stop());

        Assert.Equal("Stop failed.", exception.Message);
        Assert.Equal(SessionStatus.Inactive, fixture.Controller.Snapshot.Status);
        Assert.False(fixture.Timer.IsRunning);
        Assert.Equal(SessionEndReason.NativeError, completed?.EndReason);
        Assert.Equal("Stop failed.", completed?.ErrorMessage);
    }

    [Fact]
    public void ShutdownUsesApplicationExitReason()
    {
        using SessionFixture fixture = new();
        SessionRecord? completed = null;
        fixture.Controller.SessionCompleted += (_, e) => completed = e.Session;
        fixture.Controller.Start(TimeSpan.FromHours(1), simulateInputActivity: false);

        fixture.Controller.Shutdown();

        Assert.Equal(SessionEndReason.ApplicationExit, completed?.EndReason);
        Assert.False(fixture.KeepAwake.IsActive);
    }

    private sealed class SessionFixture : IDisposable
    {
        public SessionFixture()
        {
            KeepAwake = new FakeKeepAwakeService();
            Clock = new FakeClock(new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero));
            Timer = new FakeSessionTimer();
            ActivitySimulator = new FakeActivityInputSimulator();
            Controller = new SessionController(KeepAwake, Clock, Timer, ActivitySimulator);
        }

        public FakeKeepAwakeService KeepAwake { get; }

        public FakeClock Clock { get; }

        public FakeSessionTimer Timer { get; }

        public FakeActivityInputSimulator ActivitySimulator { get; }

        public SessionController Controller { get; }

        public void Dispose()
        {
            KeepAwake.ThrowOnStop = false;
            Controller.Dispose();
        }
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }

    private sealed class FakeSessionTimer : ISessionTimer
    {
        public event EventHandler? Tick;

        public bool IsRunning { get; private set; }

        public void Start() => IsRunning = true;

        public void Stop() => IsRunning = false;

        public void Fire()
        {
            if (IsRunning)
            {
                Tick?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose() => IsRunning = false;
    }

    private sealed class FakeKeepAwakeService : IKeepAwakeService
    {
        public bool IsActive { get; private set; }

        public bool ThrowOnStart { get; set; }

        public bool ThrowOnStop { get; set; }

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public void Start()
        {
            StartCalls++;
            if (ThrowOnStart)
            {
                throw new KeepAwakeException("Start failed.");
            }

            IsActive = true;
        }

        public void Stop()
        {
            StopCalls++;
            if (ThrowOnStop)
            {
                throw new KeepAwakeException("Stop failed.");
            }

            IsActive = false;
        }
    }

    private sealed class FakeActivityInputSimulator : IActivityInputSimulator
    {
        public bool IsActive { get; private set; }

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public void Start()
        {
            StartCalls++;
            IsActive = true;
        }

        public void Stop()
        {
            StopCalls++;
            IsActive = false;
        }
    }
}
