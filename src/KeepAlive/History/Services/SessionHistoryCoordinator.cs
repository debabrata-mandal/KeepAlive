using System.IO;
using KeepAlive.Core;
using KeepAlive.Core.Events;
using KeepAlive.Core.Models;

namespace KeepAlive.History.Services;

public sealed class SessionHistoryCoordinator : IDisposable
{
    private readonly ISessionHistoryStore _store;
    private readonly ISessionController _sessionController;
    private Guid? _savedActiveSessionId;

    public SessionHistoryCoordinator(ISessionHistoryStore store, ISessionController sessionController, IClock clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        ArgumentNullException.ThrowIfNull(clock);

        Execute(() => _store.RecoverInterrupted(clock.UtcNow));
        _sessionController.StateChanged += OnStateChanged;
        _sessionController.SessionCompleted += OnSessionCompleted;
    }

    public event EventHandler? HistoryChanged;

    public event EventHandler<HistoryErrorEventArgs>? HistoryError;

    public string? LastError { get; private set; }

    public IReadOnlyList<SessionRecord> GetHistory()
    {
        try
        {
            IReadOnlyList<SessionRecord> sessions = _store.ReadAll();
            LastError = null;
            return sessions;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ReportError("Session history could not be read.");
            return Array.Empty<SessionRecord>();
        }
    }

    public void Clear()
    {
        Execute(_store.Clear);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _sessionController.StateChanged -= OnStateChanged;
        _sessionController.SessionCompleted -= OnSessionCompleted;
    }

    private void OnStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        if (e.Snapshot.Status != SessionStatus.Active || e.Snapshot.ActiveSession is null)
        {
            return;
        }

        if (_savedActiveSessionId == e.Snapshot.ActiveSession.Id)
        {
            return;
        }

        _savedActiveSessionId = e.Snapshot.ActiveSession.Id;
        Execute(() => _store.Upsert(e.Snapshot.ActiveSession));
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessionCompleted(object? sender, SessionCompletedEventArgs e)
    {
        Execute(() => _store.Upsert(e.Session));
        _savedActiveSessionId = null;
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Execute(Action action)
    {
        try
        {
            action();
            LastError = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ReportError("Session history could not be saved.");
        }
    }

    private void ReportError(string message)
    {
        LastError = message;
        HistoryError?.Invoke(this, new HistoryErrorEventArgs(message));
    }
}
