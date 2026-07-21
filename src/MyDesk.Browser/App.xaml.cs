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
                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: UserDataFolder,
                    options: new CoreWebView2EnvironmentOptions()
                    {
                        AdditionalBrowserArguments = "--disable-web-security --disable-features=VizDisplayCompositor"
                    });

                // Store the environment for use in MainWindow
                Current.Properties["WebView2Environment"] = environment;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Initialize ViewModels
            Current.Properties["MainViewModel"] = new MainViewModel();
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