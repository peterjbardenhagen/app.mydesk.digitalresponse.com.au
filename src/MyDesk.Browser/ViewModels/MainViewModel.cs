using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace MyDesk.Browser.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly AppSettings _settings;
        

        [ObservableProperty]
        private string _initialUrl = "https://www.office.com";

        [ObservableProperty]
        private string _currentUrl = "https://www.office.com";

        [ObservableProperty]
        private string _windowTitle = "MyDesk Browser";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _canGoBack = false;

        [ObservableProperty]
        private bool _canGoForward = false;

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private double _progressValue = 0;

        [ObservableProperty]
        private bool _isProgressVisible = false;

        [ObservableProperty]
        private bool _showUrlBar = true;

        [ObservableProperty]
        private WindowState _savedWindowState = WindowState.Normal;

        [ObservableProperty]
        private double _savedWidth = 1400;

        [ObservableProperty]
        private double _savedHeight = 900;

        [ObservableProperty]
        private double _savedLeft = 100;

        [ObservableProperty]
        private double _savedTop = 100;

        public MainViewModel() : this(null, null) { }

        public MainViewModel(ILogger<MainViewModel> logger, AppSettings settings)
        {
            _logger = logger;
            _settings = settings ?? new AppSettings();

            // Initialize URLs from settings
            _initialUrl = _settings.DefaultUrl ?? "https://app.mydesk.digitalresponse.com.au";
            _currentUrl = _initialUrl;
            _windowTitle = _settings.WindowTitle ?? "MyDesk Browser";

            // Prepare persistent user data folder for WebView2
            InitializeWebViewProperties();

            // Load saved window state
            LoadWindowState();
        }

        public string UserDataFolder { get; private set; } = string.Empty;

        private void InitializeWebViewProperties()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            UserDataFolder = Path.Combine(appData, "MyDesk", "Browser");
            // Ensure the folder exists for WebView2 persistence
            if (!Directory.Exists(UserDataFolder))
            {
                Directory.CreateDirectory(UserDataFolder);
            }
        }

        // Expose settings for UI consumption
        public AppSettings Settings => _settings;

        public void UpdateNavigationState(CoreWebView2 webView)
        {
            if (webView != null)
            {
                CanGoBack = webView.CanGoBack;
                CanGoForward = webView.CanGoForward;
                CurrentUrl = webView.Source ?? InitialUrl;
            }
        }

        public void SetError(string message)
        {
            HasError = true;
            ErrorMessage = message;
            IsLoading = false;
            IsProgressVisible = false;
            _logger?.LogError(message);
        }

        public void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }

        public void SaveWindowState(Window window)
        {
            try
            {
                SavedWindowState = window.WindowState;
                if (window.WindowState == WindowState.Normal)
                {
                    SavedWidth = window.Width;
                    SavedHeight = window.Height;
                    SavedLeft = window.Left;
                    SavedTop = window.Top;
                }

                // Persist settings
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MyDesk",
                    "Browser",
                    "appsettings.json");
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                _settings.WindowWidth = (int)SavedWidth;
                _settings.WindowHeight = (int)SavedHeight;
                _settings.WindowLeft = (int)SavedLeft;
                _settings.WindowTop = (int)SavedTop;
                _settings.WindowState = SavedWindowState.ToString();
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save window state");
            }
        }

        public void LoadWindowState()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MyDesk",
                    "Browser",
                    "appsettings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        SavedWidth = settings.WindowWidth > 0 ? settings.WindowWidth : 1400;
                        SavedHeight = settings.WindowHeight > 0 ? settings.WindowHeight : 900;
                        SavedLeft = settings.WindowLeft;
                        SavedTop = settings.WindowTop;
                        if (Enum.TryParse<WindowState>(settings.WindowState, out var parsedState))
                        {
                            SavedWindowState = parsedState;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load window state");
            }
        }

        public void ApplyWindowState(Window window)
        {
            try
            {
                window.Width = SavedWidth;
                window.Height = SavedHeight;
                window.Left = SavedLeft;
                window.Top = SavedTop;
                window.WindowState = SavedWindowState;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to apply window state");
            }
        }

        public void Dispose() { }
    }

    public class AppSettings
    {
        public string DefaultUrl { get; set; } = "https://app.mydesk.digitalresponse.com.au";
        public string WindowTitle { get; set; } = "MyDesk Browser";
        public int WindowWidth { get; set; } = 1400;
        public int WindowHeight { get; set; } = 900;
        public int WindowLeft { get; set; } = 100;
        public int WindowTop { get; set; } = 100;
        public string WindowState { get; set; } = "Normal";
        public bool EnableDevTools { get; set; } = false;
        public bool AllowExternalLinks { get; set; } = true;
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0";
        public bool StartMaximized { get; set; } = false;
        public bool RememberWindowState { get; set; } = true;
        public bool HardwareAcceleration { get; set; } = true;
    }
}