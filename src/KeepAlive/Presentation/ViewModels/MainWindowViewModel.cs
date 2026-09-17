using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using KeepAlive.Core;
using KeepAlive.Core.Events;
using KeepAlive.Core.Models;
using KeepAlive.Presentation.Commands;
using KeepAlive.Presentation.Models;

namespace KeepAlive.Presentation.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISessionController _sessionController;

    private string _countdownText = "00:00:00";
    private string _customMinutesText = "60";
    private string? _durationValidationMessage;
    private string? _errorMessage;
    private string _endTimeText = string.Empty;
    private bool _isActive;
    private DurationOption _selectedDurationOption;
    private int _selectedTabIndex;

    public MainWindowViewModel(ISessionController sessionController, SettingsViewModel settings, HistoryViewModel history)
    {
        _sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        History = history ?? throw new ArgumentNullException(nameof(history));
        DurationOptions =
        [
            new("15 minutes", TimeSpan.FromMinutes(15)),
            new("30 minutes", TimeSpan.FromMinutes(30)),
            new("1 hour", TimeSpan.FromHours(1)),
            new("2 hours", TimeSpan.FromHours(2)),
            new("4 hours", TimeSpan.FromHours(4)),
            new("Custom", null),
        ];
        _selectedDurationOption = DurationOptions.Single(option => option.Duration == SessionDurationPolicy.Default);

        StartCommand = new RelayCommand(StartSession, CanStartSession);
        StopCommand = new RelayCommand(StopSession, () => IsActive);
        ApplyDefaultDuration();

        _sessionController.StateChanged += OnStateChanged;
        _sessionController.SessionCompleted += OnSessionCompleted;
        Settings.SettingsChanged += OnSettingsChanged;
        ApplySnapshot(_sessionController.Snapshot);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DurationOption> DurationOptions { get; }

    public SettingsViewModel Settings { get; }

    public HistoryViewModel History { get; }

    public RelayCommand StartCommand { get; }

    public RelayCommand StopCommand { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetField(ref _selectedTabIndex, value);
    }

    public DurationOption SelectedDurationOption
    {
        get => _selectedDurationOption;
        set
        {
            if (SetField(ref _selectedDurationOption, value))
            {
                OnPropertyChanged(nameof(IsCustomDuration));
                ValidateCustomDuration();
                StartCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string CustomMinutesText
    {
        get => _customMinutesText;
        set
        {
            if (SetField(ref _customMinutesText, value))
            {
                ValidateCustomDuration();
                StartCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsCustomDuration => SelectedDurationOption.IsCustom;

    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (SetField(ref _isActive, value))
            {
                OnPropertyChanged(nameof(IsInactive));
            }
        }
    }

    public bool IsInactive => !IsActive;

    public string CountdownText
    {
        get => _countdownText;
        private set => SetField(ref _countdownText, value);
    }

    public string EndTimeText
    {
        get => _endTimeText;
        private set => SetField(ref _endTimeText, value);
    }

    public string? DurationValidationMessage
    {
        get => _durationValidationMessage;
        private set => SetField(ref _durationValidationMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public TimeSpan? SelectedDuration
    {
        get
        {
            if (SelectedDurationOption.Duration is { } preset)
            {
                return preset;
            }

            return int.TryParse(CustomMinutesText, out int minutes) && minutes is >= 1 and <= 480
                ? TimeSpan.FromMinutes(minutes)
                : null;
        }
    }

    public void Dispose()
    {
        _sessionController.StateChanged -= OnStateChanged;
        _sessionController.SessionCompleted -= OnSessionCompleted;
        Settings.SettingsChanged -= OnSettingsChanged;
        History.Dispose();
    }

    public void ShowSession() => SelectedTabIndex = 0;

    public void ShowHistory()
    {
        History.Refresh();
        SelectedTabIndex = 1;
    }

    public void ShowSettings() => SelectedTabIndex = 2;

    private bool CanStartSession() => IsInactive && SelectedDuration is not null;

    private void StartSession()
    {
        if (SelectedDuration is not { } duration)
        {
            ValidateCustomDuration();
            return;
        }

        ErrorMessage = null;

        try
        {
            _sessionController.Start(duration, Settings.SimulateInputActivity);
        }
        catch (Exception exception) when (exception is KeepAwakeException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void StopSession()
    {
        ErrorMessage = null;

        try
        {
            _sessionController.Stop();
        }
        catch (KeepAwakeException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void OnStateChanged(object? sender, SessionStateChangedEventArgs e) => ApplySnapshot(e.Snapshot);

    private void OnSessionCompleted(object? sender, SessionCompletedEventArgs e)
    {
        if (e.Session.EndReason == SessionEndReason.NativeError)
        {
            ErrorMessage = e.Session.ErrorMessage ?? "Windows could not update the keep-awake request.";
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (IsInactive)
        {
            ApplyDefaultDuration();
        }
    }

    private void ApplyDefaultDuration()
    {
        int defaultMinutes = Settings.CurrentSettings.DefaultDurationMinutes;
        DurationOption? preset = DurationOptions.FirstOrDefault(option => option.Duration?.TotalMinutes == defaultMinutes);

        if (preset is not null)
        {
            SelectedDurationOption = preset;
            return;
        }

        SelectedDurationOption = DurationOptions.Single(option => option.IsCustom);
        CustomMinutesText = defaultMinutes.ToString(CultureInfo.InvariantCulture);
    }

    private void ApplySnapshot(SessionSnapshot snapshot)
    {
        IsActive = snapshot.Status == SessionStatus.Active;

        if (IsActive && snapshot.ActiveSession is not null)
        {
            CountdownText = FormatDuration(snapshot.Remaining);
            EndTimeText = $"Stops automatically at {snapshot.ActiveSession.PlannedEndUtc.ToLocalTime():h:mm tt}";
        }
        else
        {
            CountdownText = "00:00:00";
            EndTimeText = string.Empty;
        }

        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    private void ValidateCustomDuration()
    {
        if (!IsCustomDuration)
        {
            DurationValidationMessage = null;
            return;
        }

        DurationValidationMessage = SelectedDuration is null ? "Enter a whole number from 1 to 480 minutes." : null;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        long wholeSeconds = Math.Max(0, (long)Math.Ceiling(duration.TotalSeconds));
        return TimeSpan.FromSeconds(wholeSeconds).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
