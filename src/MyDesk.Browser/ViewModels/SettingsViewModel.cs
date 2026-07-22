using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace MyDesk.Browser.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ILogger<SettingsViewModel>? _logger;
        private readonly AppSettings _settings;
        private readonly string _settingsPath;

        [ObservableProperty]
        private string _defaultUrl = "https://app.mydesk.digitalresponse.com.au";

        [ObservableProperty]
        private string _windowTitle = "MyDesk Browser";

        [ObservableProperty]
        private int _windowWidth = 1400;

        [ObservableProperty]
        private int _windowHeight = 900;

        [ObservableProperty]
        private bool _startMaximized = false;

        [ObservableProperty]
        private bool _rememberWindowState = true;

        [ObservableProperty]
        private bool _hardwareAcceleration = true;

        [ObservableProperty]
        private bool _enableDevTools = false;

        [ObservableProperty]
        private bool _allowExternalLinks = true;

        [ObservableProperty]
        private string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        [ObservableProperty]
        private bool _showToolbar = true;

        [ObservableProperty]
        private bool _autoGrantPermissions = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Application version from the assembly.
        /// </summary>
        public string Version =>
            System.Reflection.Assembly.GetExecutingAssembly().GetName()?.Version?.ToString() ?? "1.0.0";

        [ObservableProperty]
        private bool _isSaving = false;

        public SettingsViewModel() : this(null!)
        {
        }

        public SettingsViewModel(ILogger<SettingsViewModel>? logger)
        {
            _logger = logger;
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MyDesk",
                "Browser",
                "appsettings.json");

            _settings = LoadSettings();
            LoadFromSettings();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load settings");
            }
            return new AppSettings();
        }

        private void LoadFromSettings()
        {
            DefaultUrl = _settings.DefaultUrl;
            WindowTitle = _settings.WindowTitle;
            WindowWidth = _settings.WindowWidth;
            WindowHeight = _settings.WindowHeight;
            StartMaximized = _settings.StartMaximized;
            RememberWindowState = _settings.RememberWindowState;
            HardwareAcceleration = _settings.HardwareAcceleration;
            EnableDevTools = _settings.EnableDevTools;
            AllowExternalLinks = _settings.AllowExternalLinks;
            UserAgent = _settings.UserAgent;
            ShowToolbar = _settings.ShowToolbar;
            AutoGrantPermissions = _settings.AutoGrantPermissions;
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsSaving = true;
            StatusMessage = "Saving...";

            try
            {
                _settings.DefaultUrl = DefaultUrl;
                _settings.WindowTitle = WindowTitle;
                _settings.WindowWidth = WindowWidth;
                _settings.WindowHeight = WindowHeight;
                _settings.StartMaximized = StartMaximized;
                _settings.RememberWindowState = RememberWindowState;
                _settings.HardwareAcceleration = HardwareAcceleration;
                _settings.EnableDevTools = EnableDevTools;
                _settings.AllowExternalLinks = AllowExternalLinks;
                _settings.UserAgent = UserAgent;
                _settings.ShowToolbar = ShowToolbar;
                _settings.AutoGrantPermissions = AutoGrantPermissions;

                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);

                StatusMessage = "Settings saved successfully!";
                _logger?.LogInformation("Settings saved to {Path}", _settingsPath);

                // Reset status after 3 seconds
                await System.Threading.Tasks.Task.Delay(3000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logger?.LogError(ex, "Failed to save settings");
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            DefaultUrl = "https://app.mydesk.digitalresponse.com.au";
            WindowTitle = "MyDesk Browser";
            WindowWidth = 1400;
            WindowHeight = 900;
            StartMaximized = false;
            RememberWindowState = true;
            HardwareAcceleration = true;
            EnableDevTools = false;
            AllowExternalLinks = true;
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

            StatusMessage = "Reset to defaults. Click Save to apply.";
        }

        [RelayCommand]
        private void OpenSettingsFolder()
        {
            try
            {
                var folder = Path.GetDirectoryName(_settingsPath);
                if (Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening folder: {ex.Message}";
                _logger?.LogError(ex, "Failed to open settings folder");
            }
        }

        [RelayCommand]
        private void ClearCache()
        {
            try
            {
                var cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MyDesk",
                    "Browser",
                    "WebView2");

                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                    StatusMessage = "Cache cleared successfully!";
                }
                else
                {
                    StatusMessage = "Cache folder not found.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error clearing cache: {ex.Message}";
                _logger?.LogError(ex, "Failed to clear cache");
            }
        }
    }
}
