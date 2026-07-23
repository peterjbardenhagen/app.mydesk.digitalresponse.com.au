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
        private readonly ILogger<MainViewModel>? _logger;
        private readonly AppSettings _settings;

        [ObservableProperty]
        private string _initialUrl = "https://app.mydesk.digitalresponse.com.au";

        [ObservableProperty]
        private string _currentUrl = "https://app.mydesk.digitalresponse.com.au";

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

        // ── Auth State ───────────────────────────────────────────────────────

        [ObservableProperty]
        private bool _isAuthenticated = false;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _userEmail = string.Empty;

        [ObservableProperty]
        private string _userAvatarUrl = string.Empty;

        [ObservableProperty]
        private bool _isAgentsOnline = false;

        /// <summary>
        /// Computed initials from the user's name for the avatar circle.
        /// </summary>
        public string UserInitials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UserName)) return "?";
                var parts = UserName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
                return UserName.Length >= 1 ? UserName[..1].ToUpper() : "?";
            }
        }

        partial void OnUserNameChanged(string value)
        {
            OnPropertyChanged(nameof(UserInitials));
        }

        // The WebView2 instance (set by MainWindow after initialization)
        private WebView2? _webView;

        public MainViewModel() : this(null, null) { }

        public MainViewModel(ILogger<MainViewModel>? logger, AppSettings? settings)
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

            // Load saved user info
            _userName = _settings.LastUserName ?? string.Empty;
            _userEmail = _settings.LastUserEmail ?? string.Empty;
        }

        public string UserDataFolder { get; private set; } = string.Empty;

        /// <summary>
        /// Called by MainWindow after WebView2 is initialized to store a reference.
        /// </summary>
        public void SetWebView(WebView2 webView)
        {
            _webView = webView;
        }

        private void InitializeWebViewProperties()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            UserDataFolder = Path.Combine(appData, "MyDesk", "Browser");
            if (!Directory.Exists(UserDataFolder))
            {
                Directory.CreateDirectory(UserDataFolder);
            }
        }

        // Expose settings for UI consumption
        public AppSettings Settings => _settings;

        /// <summary>
        /// Called after every page navigation to detect auth state.
        /// Injects JavaScript to check for auth cookies and user info.
        /// </summary>
        public async Task CheckAuthStateAsync()
        {
            if (_webView?.CoreWebView2 == null) return;

            try
            {
                // JavaScript to check auth state from the page
                var script = @"
                    (function() {
                        // Check for auth cookie
                        var hasCookie = document.cookie.indexOf('.AspNetCore') >= 0 || document.cookie.indexOf('Identity') >= 0;

                        // Try to extract user info from common DOM patterns
                        var userName = '';
                        var userEmail = '';

                        // Pattern 1: MudBlazor user menu button
                        var userBtn = document.querySelector('[data-user-name]');
                        if (userBtn) userName = userBtn.getAttribute('data-user-name');

                        // Pattern 2: Common user display elements
                        if (!userName) {
                            var els = document.querySelectorAll('.user-name, .user-display-name, [class*=user][class*=name]');
                            if (els.length > 0) userName = els[0].textContent.trim();
                        }

                        // Pattern 3: Meta tag
                        if (!userName) {
                            var meta = document.querySelector('meta[name=""user-name""]');
                            if (meta) userName = meta.getAttribute('content');
                        }

                        // Email patterns
                        var emailMeta = document.querySelector('meta[name=""user-email""]');
                        if (emailMeta) userEmail = emailMeta.getAttribute('content');

                        // Check if we're on a login page
                        var isLoginPage = window.location.pathname.indexOf('/login') === 0 || 
                                         window.location.pathname.indexOf('/Account/Login') === 0;

                        // Check if the page content suggests we're logged in
                        var hasAppContent = document.querySelector('.mud-layout, .main-layout, #app, [class*=dashboard]') !== null;

                        // AgentsOS status detection
                        var isAgentsOnline = false;
                        var agentsAlert = document.querySelector('.mud-alert-severity-success');
                        if (agentsAlert && window.location.pathname.indexOf('/agentsos') >= 0) {
                            isAgentsOnline = agentsAlert.textContent.indexOf('reachable') >= 0;
                        }

                        return JSON.stringify({
                            isAuthenticated: (hasCookie || hasAppContent) && !isLoginPage,
                            userName: userName,
                            userEmail: userEmail,
                            path: window.location.pathname,
                            hasCookie: hasCookie,
                            isLoginPage: isLoginPage,
                            hasAppContent: hasAppContent,
                            isAgentsOnline: isAgentsOnline
                        });
                    })();
                ";

                var jsonResult = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                var result = JsonSerializer.Deserialize<JsonElement>(jsonResult);

                if (result.ValueKind == JsonValueKind.Object)
                {
                    var isAuthed = result.TryGetProperty("isAuthenticated", out var authProp) && authProp.GetBoolean();
                    var name = result.TryGetProperty("userName", out var nameProp) ? nameProp.GetString() ?? "" : "";
                    var email = result.TryGetProperty("userEmail", out var emailProp) ? emailProp.GetString() ?? "" : "";
                    var agentsOnline = result.TryGetProperty("isAgentsOnline", out var agentsProp) && agentsProp.GetBoolean();

                    IsAuthenticated = isAuthed;
                    IsAgentsOnline = agentsOnline;
                    if (isAuthed && !string.IsNullOrEmpty(name))
                    {
                        UserName = name;
                        UserEmail = email;
                        _settings.LastUserName = name;
                        _settings.LastUserEmail = email;
                    }
                    else if (!isAuthed)
                    {
                        UserName = string.Empty;
                        UserEmail = string.Empty;
                    }

                    UpdateTitle();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Auth state detection failed (expected on non-MyDesk pages)");
            }
        }

        /// <summary>
        /// Updates the window title to reflect auth state.
        /// </summary>
        public void UpdateTitle()
        {
            if (IsAuthenticated && !string.IsNullOrEmpty(UserName))
            {
                WindowTitle = $"{_settings.WindowTitle} — {UserName}";
            }
            else
            {
                WindowTitle = _settings.WindowTitle;
            }
        }

        /// <summary>
        /// Logs the user out by clearing cookies and navigating to the logout page.
        /// </summary>
        public async Task LogoutAsync()
        {
            if (_webView?.CoreWebView2 == null) return;

            try
            {
                // Clear all cookies for the current domain
                var cookieManager = _webView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync(CurrentUrl);
                foreach (var cookie in cookies)
                {
                    cookieManager.DeleteCookie(cookie);
                }

                // Clear all cookies for *.digitalresponse.com.au
                var drCookies = await cookieManager.GetCookiesAsync("https://digitalresponse.com.au");
                foreach (var cookie in drCookies)
                {
                    cookieManager.DeleteCookie(cookie);
                }

                IsAuthenticated = false;
                UserName = string.Empty;
                UserEmail = string.Empty;
                _settings.LastUserName = null;
                _settings.LastUserEmail = null;
                PersistSettings();
                UpdateTitle();

                // Navigate to logout page
                _webView.CoreWebView2.Navigate("https://app.mydesk.digitalresponse.com.au/logout");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Logout failed");
            }
        }

        /// <summary>
        /// Navigates to the login page.
        /// </summary>
        public void Login()
        {
            _webView?.CoreWebView2?.Navigate("https://app.mydesk.digitalresponse.com.au/login");
        }

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

        /// <summary>
        /// Persists the current AppSettings to disk (without window state).
        /// Used to save auth-related settings on logout so stale data doesn't
        /// reappear on next launch.
        /// </summary>
        private void PersistSettings()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MyDesk",
                    "Browser",
                    "appsettings.json");
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist settings");
            }
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
                _settings.WindowWidth = (int)SavedWidth;
                _settings.WindowHeight = (int)SavedHeight;
                _settings.WindowLeft = (int)SavedLeft;
                _settings.WindowTop = (int)SavedTop;
                _settings.WindowState = SavedWindowState.ToString();
                PersistSettings();
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
                    var persisted = JsonSerializer.Deserialize<AppSettings>(json);
                    if (persisted != null)
                    {
                        SavedWidth = persisted.WindowWidth > 0 ? persisted.WindowWidth : 1400;
                        SavedHeight = persisted.WindowHeight > 0 ? persisted.WindowHeight : 900;
                        SavedLeft = persisted.WindowLeft;
                        SavedTop = persisted.WindowTop;
                        if (Enum.TryParse<WindowState>(persisted.WindowState, out var parsedState))
                        {
                            SavedWindowState = parsedState;
                        }

                        // Restore saved user info so the UI shows the previous
                        // session's identity immediately, even before auth re-check.
                        if (!string.IsNullOrEmpty(persisted.LastUserName))
                        {
                            _settings.LastUserName = persisted.LastUserName;
                            _userName = persisted.LastUserName;
                        }
                        if (!string.IsNullOrEmpty(persisted.LastUserEmail))
                        {
                            _settings.LastUserEmail = persisted.LastUserEmail;
                            _userEmail = persisted.LastUserEmail;
                        }

                        // Push restored user info into the title bar and initials.
                        UpdateTitle();
                        OnPropertyChanged(nameof(UserInitials));
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
        public bool ShowToolbar { get; set; } = true;
        public bool AutoGrantPermissions { get; set; } = true;

        // Auth-related persisted settings
        public string? LastUserName { get; set; }
        public string? LastUserEmail { get; set; }
    }
}
