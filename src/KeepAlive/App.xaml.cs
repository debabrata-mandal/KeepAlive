using System.Windows;
using KeepAlive.Infrastructure;

namespace KeepAlive;

public partial class App : Application
{
    private SingleInstanceCoordinator? _singleInstance;

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

        _singleInstance.ActivationRequested += (_, _) =>
            Dispatcher.BeginInvoke(ActivateMainWindow);
        _singleInstance.StartListening();

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
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
