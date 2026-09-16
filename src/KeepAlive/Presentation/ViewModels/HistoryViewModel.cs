using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KeepAlive.Core.Models;
using KeepAlive.History.Services;

namespace KeepAlive.Presentation.ViewModels;

public sealed class HistoryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SessionHistoryCoordinator _historyCoordinator;
    private string? _errorMessage;
    private string _summaryText = "No sessions recorded yet.";

    public HistoryViewModel(SessionHistoryCoordinator historyCoordinator)
    {
        _historyCoordinator = historyCoordinator ?? throw new ArgumentNullException(nameof(historyCoordinator));
        _historyCoordinator.HistoryChanged += OnHistoryChanged;
        _historyCoordinator.HistoryError += OnHistoryError;
        Refresh();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SessionHistoryItemViewModel> Sessions { get; } = [];

    public string SummaryText
    {
        get => _summaryText;
        private set => SetField(ref _summaryText, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public bool CanClear => Sessions.Count > 0;

    public void Refresh()
    {
        IReadOnlyList<SessionRecord> sessions = _historyCoordinator.GetHistory();
        Sessions.Clear();

        foreach (SessionRecord session in sessions)
        {
            Sessions.Add(new SessionHistoryItemViewModel(session));
        }

        SummaryText = Sessions.Count == 0 ? "No sessions recorded yet." : $"{Sessions.Count} session{(Sessions.Count == 1 ? string.Empty : "s")} recorded";
        ErrorMessage = _historyCoordinator.LastError;
        OnPropertyChanged(nameof(CanClear));
    }

    public void Clear()
    {
        _historyCoordinator.Clear();
        Refresh();
    }

    public void Dispose()
    {
        _historyCoordinator.HistoryChanged -= OnHistoryChanged;
        _historyCoordinator.HistoryError -= OnHistoryError;
    }

    private void OnHistoryChanged(object? sender, EventArgs e) => Refresh();

    private void OnHistoryError(object? sender, HistoryErrorEventArgs e) => ErrorMessage = e.Message;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
