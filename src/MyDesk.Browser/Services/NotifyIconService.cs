using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using MyDesk.Browser.Views;

namespace MyDesk.Browser.Services;

/// <summary>
/// System tray notification service for MyDesk Browser.
/// Handles notification tray icon, balloon tips, and background alerts
/// when the main window is minimized or closed to tray.
/// </summary>
public sealed class NotifyIconService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly System.Windows.Forms.NotifyIcon _trayIcon;
    private readonly DispatcherTimer _alertTimer;
    private bool _disposed;

    public NotifyIconService(Window mainWindow)
    {
        _mainWindow = mainWindow;
        _trayIcon = new System.Windows.Forms.NotifyIcon();

        using var iconStream = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/icon.ico"))?.Stream;

        if (iconStream != null)
        {
            _trayIcon.Icon = new Icon(iconStream);
        }

        _trayIcon.Text = "MyDesk Browser";
        _trayIcon.Visible = true;

        _trayIcon.Click += (_, _) => RestoreMainWindow();

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Open MyDesk", null, (_, _) => RestoreMainWindow());
        contextMenu.Items.Add("Support", null, (_, _) => OpenSupport());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = contextMenu;

        mainWindow.StateChanged += OnMainWindowStateChanged;

        _alertTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _alertTimer.Tick += async (_, _) => await CheckForNotifications();
    }

    public void ShowNotification(string title, string message,
        System.Windows.Forms.ToolTipIcon icon = System.Windows.Forms.ToolTipIcon.Info)
    {
        _trayIcon.ShowBalloonTip(5000, title, message, icon);
    }

    public void StartBackgroundAlerts()
    {
        _alertTimer.Start();
    }

    public void StopBackgroundAlerts()
    {
        _alertTimer.Stop();
    }

    private void RestoreMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void OpenSupport()
    {
        RestoreMainWindow();

        // Open the support dialog (consistent with main menu behavior).
        // Need to get the user name and support email from the MainViewModel.
        if (System.Windows.Application.Current.Properties["MainViewModel"] is ViewModels.MainViewModel vm)
        {
            var supportWindow = new Views.SupportWindow(vm.UserName, vm.Settings.SupportEmail)
            {
                Owner = _mainWindow
            };
            supportWindow.ShowDialog();
        }
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }

    private void OnMainWindowStateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.Hide();
            ShowNotification("MyDesk Browser", "App is running in the background. " +
                "You will receive notifications here.");
        }
    }

    private async Task CheckForNotifications()
    {
        // Future: poll backend API for unread notification count
        // Phase 7.1: placeholder - will be wired to backend notification endpoint
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _alertTimer.Stop();
        _mainWindow.StateChanged -= OnMainWindowStateChanged;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
