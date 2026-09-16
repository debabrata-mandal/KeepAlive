using System.Drawing;
using KeepAlive.Core;
using KeepAlive.Core.Events;
using KeepAlive.Core.Models;
using KeepAlive.Presentation.Notifications;
using Forms = System.Windows.Forms;

namespace KeepAlive.Presentation.Services;

public sealed class TrayIconService : IUserNotificationService, IDisposable
{
    private readonly ISessionController _sessionController;
    private readonly Func<TimeSpan?> _selectedDuration;
    private readonly Action _showMainWindow;
    private readonly Icon _activeIcon;
    private readonly Icon _inactiveIcon;
    private readonly Forms.ToolStripMenuItem _startItem;
    private readonly Forms.ToolStripMenuItem _stopItem;
    private readonly Forms.NotifyIcon _trayIcon;

    public TrayIconService(
        ISessionController sessionController,
        Func<TimeSpan?> selectedDuration,
        Action showMainWindow,
        Action showHistory,
        Action showSettings,
        Action exitApplication)
    {
        _sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        _selectedDuration = selectedDuration ?? throw new ArgumentNullException(nameof(selectedDuration));
        _showMainWindow = showMainWindow ?? throw new ArgumentNullException(nameof(showMainWindow));
        ArgumentNullException.ThrowIfNull(showHistory);
        ArgumentNullException.ThrowIfNull(showSettings);
        ArgumentNullException.ThrowIfNull(exitApplication);

        _activeIcon = LoadIcon("keep-alive-active.ico");
        _inactiveIcon = LoadIcon("keep-alive-inactive.ico");

        Forms.ToolStripMenuItem openItem = new("Open", null, (_, _) => _showMainWindow());
        _startItem = new Forms.ToolStripMenuItem("Start session", null, (_, _) => StartSession());
        _stopItem = new Forms.ToolStripMenuItem("Stop session", null, (_, _) => StopSession());
        Forms.ToolStripMenuItem historyItem = new("History", null, (_, _) => showHistory());
        Forms.ToolStripMenuItem settingsItem = new("Settings", null, (_, _) => showSettings());
        Forms.ToolStripMenuItem exitItem = new("Exit", null, (_, _) => exitApplication());

        Forms.ContextMenuStrip contextMenu = new();
        contextMenu.Items.AddRange(
        [
            openItem,
            new Forms.ToolStripSeparator(),
            _startItem,
            _stopItem,
            new Forms.ToolStripSeparator(),
            historyItem,
            settingsItem,
            new Forms.ToolStripSeparator(),
            exitItem,
        ]);

        _trayIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = contextMenu,
            Icon = _inactiveIcon,
            Text = "Keep Alive - inactive",
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => _showMainWindow();

        _sessionController.StateChanged += OnStateChanged;
        UpdateState(_sessionController.Snapshot);
    }

    public void Show(UserNotification notification)
    {
        Forms.ToolTipIcon icon = notification.Kind switch
        {
            NotificationKind.Warning => Forms.ToolTipIcon.Warning,
            NotificationKind.Error => Forms.ToolTipIcon.Error,
            _ => Forms.ToolTipIcon.Info,
        };

        _trayIcon.ShowBalloonTip(4_000, notification.Title, notification.Message, icon);
    }

    public void Dispose()
    {
        _sessionController.StateChanged -= OnStateChanged;
        _trayIcon.Visible = false;
        _trayIcon.ContextMenuStrip?.Dispose();
        _trayIcon.Dispose();
        _activeIcon.Dispose();
        _inactiveIcon.Dispose();
    }

    private void StartSession()
    {
        if (_selectedDuration() is not { } duration)
        {
            Show(new UserNotification("Keep Alive", "Choose a valid duration in the main window.", NotificationKind.Warning));
            _showMainWindow();
            return;
        }

        try
        {
            _sessionController.Start(duration);
        }
        catch (Exception exception) when (exception is KeepAwakeException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            Show(new UserNotification("Keep Alive error", exception.Message, NotificationKind.Error));
        }
    }

    private void StopSession()
    {
        try
        {
            _sessionController.Stop();
        }
        catch (KeepAwakeException exception)
        {
            Show(new UserNotification("Keep Alive error", exception.Message, NotificationKind.Error));
        }
    }

    private void OnStateChanged(object? sender, SessionStateChangedEventArgs e) => UpdateState(e.Snapshot);

    private void UpdateState(SessionSnapshot snapshot)
    {
        bool isActive = snapshot.Status == SessionStatus.Active;
        _trayIcon.Icon = isActive ? _activeIcon : _inactiveIcon;
        _trayIcon.Text = isActive ? FormatActiveTooltip(snapshot.Remaining) : "Keep Alive - inactive";
        _startItem.Enabled = !isActive;
        _stopItem.Enabled = isActive;
    }

    private static string FormatActiveTooltip(TimeSpan remaining)
    {
        int hours = Math.Max(0, (int)remaining.TotalHours);
        int minutes = Math.Max(0, remaining.Minutes);
        return $"Keep Alive - {hours}h {minutes}m remaining";
    }

    private static Icon LoadIcon(string fileName)
    {
        Uri uri = new($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute);
        System.Windows.Resources.StreamResourceInfo resource = System.Windows.Application.GetResourceStream(uri)
            ?? throw new InvalidOperationException($"Tray icon resource '{fileName}' was not found.");

        using Icon icon = new(resource.Stream);
        return (Icon)icon.Clone();
    }
}
