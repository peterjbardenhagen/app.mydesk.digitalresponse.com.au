using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using MyDesk.Browser.ViewModels;

namespace MyDesk.Browser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static readonly string UserDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyDesk",
            "Browser",
            "WebView2");

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize WebView2 environment
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MyDesk", "Browser", "appsettings.json");
                var enableDevTools = false;
                try
                {
                    if (File.Exists(settingsPath))
                    {
                        var json = File.ReadAllText(settingsPath);
                        var doc = System.Text.Json.JsonDocument.Parse(json);
                        enableDevTools = doc.RootElement.TryGetProperty("EnableDevTools", out var prop) &&
                                         prop.GetBoolean();
                    }
                }
                catch { /* use default (false) */ }

                var browserArgs = enableDevTools
                    ? "--disable-web-security --disable-features=VizDisplayCompositor"
                    : "--disable-features=VizDisplayCompositor";

                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: UserDataFolder,
                    options: new CoreWebView2EnvironmentOptions()
                    {
                        AdditionalBrowserArguments = browserArgs
                    });

                Current.Properties["WebView2Environment"] = environment;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Initialize ViewModels
            var mainVm = new MainViewModel();
            Current.Properties["MainViewModel"] = mainVm;

            // Note: MainWindow is always null here because the StartupUri window
            // is created by WPF after OnStartup returns. The NotifyIconService
            // is initialized in MainWindow_Loaded in MainWindow.xaml.cs instead.
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Current.Properties["MainViewModel"] is MainViewModel vm)
            {
                vm.Dispose();
            }
            base.OnExit(e);
        }
    }
}