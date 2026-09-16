using System.Windows;
using KeepAlive.Core;
using KeepAlive.Core.Models;
using KeepAlive.History.Services;
using KeepAlive.Infrastructure;
using KeepAlive.Infrastructure.Persistence;
using KeepAlive.Presentation.Notifications;
using KeepAlive.Presentation.Services;
using KeepAlive.Presentation.ViewModels;
using KeepAlive.Settings.Services;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace KeepAlive;

public partial class App : Application
{
    private SingleInstanceCoordinator? _singleInstance;
    private SessionController? _sessionController;
    private SessionHistoryCoordinator? _historyCoordinator;
    private MainWindowViewModel? _mainWindowViewModel;
    private SessionNotificationCoordinator? _notificationCoordinator;
    private TrayIconService? _trayIconService;
    private MainWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceCoordinator("KeepAlive");

        if (!_singleInstance.IsPrimaryInstance)
        {
            await _singleInstance.NotifyPrimaryAsync();
            Shutdown();
            return;
        }

        SystemClock clock = new();
        JsonSettingsStore settingsStore = new(AppDataPaths.SettingsFile);
        WindowsStartupRegistrationService startupRegistration = new();
        SettingsViewModel settingsViewModel = new(settingsStore, startupRegistration);
        _sessionController = new SessionController(new WindowsKeepAwakeService(), clock, new DispatcherSessionTimer(Dispatcher));
        JsonSessionHistoryStore historyStore = new(AppDataPaths.HistoryFile, clock);
        _historyCoordinator = new SessionHistoryCoordinator(historyStore, _sessionController, clock);
        HistoryViewModel historyViewModel = new(_historyCoordinator);
        _mainWindowViewModel = new MainWindowViewModel(_sessionController, settingsViewModel, historyViewModel);
        _mainWindow = new MainWindow(_mainWindowViewModel);
        MainWindow = _mainWindow;
        _trayIconService = new TrayIconService(
            _sessionController,
            () => _mainWindowViewModel.SelectedDuration,
            ShowMainWindow,
            ShowHistory,
            ShowSettings,
            RequestExit);
        _notificationCoordinator = new SessionNotificationCoordinator(
            _sessionController,
            _trayIconService,
            () => settingsViewModel.NotifyBeforeEnd,
            () => settingsViewModel.NotifyOnStop);
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        _singleInstance.ActivationRequested += (_, _) => Dispatcher.BeginInvoke(ShowMainWindow);
        _singleInstance.StartListening();

        bool startedWithWindows = e.Args.Any(argument => string.Equals(argument, "--startup", StringComparison.OrdinalIgnoreCase));

        if (!startedWithWindows || !settingsViewModel.StartMinimized)
        {
            _mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _notificationCoordinator?.Dispose();
        _mainWindowViewModel?.Dispose();
        _sessionController?.Dispose();
        _historyCoordinator?.Dispose();
        _trayIconService?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Dispatcher.BeginInvoke(() => _sessionController?.HandleResume());
        }
    }

    private void ShowMainWindow()
    {
        _mainWindowViewModel?.ShowSession();
        _mainWindow?.ShowFromTray();
    }

    private void ShowHistory()
    {
        _mainWindowViewModel?.ShowHistory();
        _mainWindow?.ShowFromTray();
    }

    private void ShowSettings()
    {
        _mainWindowViewModel?.ShowSettings();
        _mainWindow?.ShowFromTray();
    }

    private void RequestExit()
    {
        if (_sessionController?.Snapshot.Status == SessionStatus.Active)
        {
            MessageBoxResult result = MessageBox.Show(
                "A keep-awake session is active. Stop the session and exit Keep Alive?",
                "Exit Keep Alive",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _mainWindow?.AllowClose();
        Shutdown();
    }
}
