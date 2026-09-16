using System.ComponentModel;
using System.Windows;
using KeepAlive.Presentation.ViewModels;

namespace KeepAlive;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Closing += OnClosing;
        StateChanged += OnWindowStateChanged;
    }

    public void ShowFromTray()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    public void AllowClose() => _allowClose = true;

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }
}
