using System.Windows;
using KeepAlive.Core;
using KeepAlive.Infrastructure;
using Microsoft.Win32;

namespace KeepAlive;

public partial class App : Application
{
    private SingleInstanceCoordinator? _singleInstance;
    private SessionController? _sessionController;

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

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        _sessionController = new SessionController(new WindowsKeepAwakeService(), new SystemClock(), new DispatcherSessionTimer(Dispatcher));
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        _singleInstance.ActivationRequested += (_, _) =>
            Dispatcher.BeginInvoke(ActivateMainWindow);
        _singleInstance.StartListening();

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _sessionController?.Dispose();
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

    private void ActivateMainWindow()
    {
        if (MainWindow is null)
        {
            return;
        }

        if (MainWindow.WindowState == WindowState.Minimized)
        {
            MainWindow.WindowState = WindowState.Normal;
        }

        MainWindow.Show();
        MainWindow.Activate();
        MainWindow.Topmost = true;
        MainWindow.Topmost = false;
        MainWindow.Focus();
    }
}
