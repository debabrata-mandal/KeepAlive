using System.Windows;
using KeepAlive.Core;
using KeepAlive.Core.Models;
using KeepAlive.Infrastructure;
using KeepAlive.Presentation.Notifications;
using KeepAlive.Presentation.Services;
using KeepAlive.Presentation.ViewModels;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace KeepAlive;

public partial class App : Application
{
    private SingleInstanceCoordinator? _singleInstance;
    private SessionController? _sessionController;
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

        _sessionController = new SessionController(new WindowsKeepAwakeService(), new SystemClock(), new DispatcherSessionTimer(Dispatcher));
        _mainWindowViewModel = new MainWindowViewModel(_sessionController);
        _mainWindow = new MainWindow(_mainWindowViewModel);
        MainWindow = _mainWindow;
        _trayIconService = new TrayIconService(_sessionController, () => _mainWindowViewModel.SelectedDuration, ShowMainWindow, RequestExit);
        _notificationCoordinator = new SessionNotificationCoordinator(_sessionController, _trayIconService);
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        _singleInstance.ActivationRequested += (_, _) => Dispatcher.BeginInvoke(ShowMainWindow);
        _singleInstance.StartListening();

        _mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _notificationCoordinator?.Dispose();
        _mainWindowViewModel?.Dispose();
        _sessionController?.Dispose();
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
