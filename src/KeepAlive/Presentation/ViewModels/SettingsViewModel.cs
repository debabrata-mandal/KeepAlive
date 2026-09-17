using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using KeepAlive.Presentation.Models;
using KeepAlive.Settings.Models;
using KeepAlive.Settings.Services;

namespace KeepAlive.Presentation.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ISettingsStore _settingsStore;
    private readonly IStartupRegistrationService _startupRegistration;

    private string? _errorMessage;
    private bool _notifyBeforeEnd;
    private bool _notifyOnStop;
    private DurationOption _selectedDefaultDuration;
    private bool _simulateInputActivity;
    private bool _startMinimized;
    private bool _startWithWindows;

    public SettingsViewModel(ISettingsStore settingsStore, IStartupRegistrationService startupRegistration)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _startupRegistration = startupRegistration ?? throw new ArgumentNullException(nameof(startupRegistration));
        DefaultDurationOptions =
        [
            new("15 minutes", TimeSpan.FromMinutes(15)),
            new("30 minutes", TimeSpan.FromMinutes(30)),
            new("1 hour", TimeSpan.FromHours(1)),
            new("2 hours", TimeSpan.FromHours(2)),
            new("4 hours", TimeSpan.FromHours(4)),
        ];

        AppSettings settings = LoadSettings();
        _startWithWindows = settings.StartWithWindows;
        _startMinimized = settings.StartMinimized;
        _notifyBeforeEnd = settings.NotifyBeforeEnd;
        _notifyOnStop = settings.NotifyOnStop;
        _simulateInputActivity = settings.SimulateInputActivity;
        _selectedDefaultDuration = DefaultDurationOptions.FirstOrDefault(option => option.Duration?.TotalMinutes == settings.DefaultDurationMinutes)
            ?? DefaultDurationOptions.Single(option => option.Duration == TimeSpan.FromHours(2));
        ReconcileStartupRegistration();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? SettingsChanged;

    public IReadOnlyList<DurationOption> DefaultDurationOptions { get; }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows == value)
            {
                return;
            }

            try
            {
                _startupRegistration.SetEnabled(value);
                _startWithWindows = value;
                OnPropertyChanged();
                SaveSettings();
            }
            catch (StartupRegistrationException exception)
            {
                ErrorMessage = exception.Message;
                OnPropertyChanged();
            }
        }
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetAndSave(ref _startMinimized, value);
    }

    public bool NotifyBeforeEnd
    {
        get => _notifyBeforeEnd;
        set => SetAndSave(ref _notifyBeforeEnd, value);
    }

    public bool NotifyOnStop
    {
        get => _notifyOnStop;
        set => SetAndSave(ref _notifyOnStop, value);
    }

    public bool SimulateInputActivity
    {
        get => _simulateInputActivity;
        set => SetAndSave(ref _simulateInputActivity, value);
    }

    public DurationOption SelectedDefaultDuration
    {
        get => _selectedDefaultDuration;
        set
        {
            if (SetField(ref _selectedDefaultDuration, value))
            {
                SaveSettings();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public AppSettings CurrentSettings => new()
    {
        DefaultDurationMinutes = (int)(SelectedDefaultDuration.Duration ?? TimeSpan.FromHours(2)).TotalMinutes,
        StartWithWindows = StartWithWindows,
        StartMinimized = StartMinimized,
        NotifyBeforeEnd = NotifyBeforeEnd,
        NotifyOnStop = NotifyOnStop,
        SimulateInputActivity = SimulateInputActivity,
    };

    private AppSettings LoadSettings()
    {
        try
        {
            return _settingsStore.Load();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = "Settings could not be loaded. Defaults are being used.";
            return new AppSettings();
        }
    }

    private void ReconcileStartupRegistration()
    {
        try
        {
            if (_startupRegistration.IsEnabled() != StartWithWindows)
            {
                _startupRegistration.SetEnabled(StartWithWindows);
            }
        }
        catch (StartupRegistrationException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void SetAndSave(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
    {
        if (SetField(ref field, value, propertyName))
        {
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(CurrentSettings);
            ErrorMessage = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = "Settings could not be saved.";
        }
        finally
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
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
