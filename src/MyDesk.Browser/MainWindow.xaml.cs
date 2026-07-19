using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using MyDesk.Browser.ViewModels;

namespace MyDesk.Browser
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            if (System.Windows.Application.Current.Properties["MainViewModel"] is MainViewModel existingVm)
            {
                _viewModel = existingVm;
            }
            else
            {
                _viewModel = new MainViewModel();
                System.Windows.Application.Current.Properties["MainViewModel"] = _viewModel;
            }
            DataContext = _viewModel;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.ApplyWindowState(this);
                var env = await CoreWebView2Environment.CreateAsync(null, _viewModel.UserDataFolder);
                InitializeWebView();
                // Navigate to the initial URL after WebView is ready
                if (!string.IsNullOrEmpty(_viewModel.InitialUrl))
                {
                    WebView.CoreWebView2?.Navigate(_viewModel.InitialUrl);
                }
            }
            catch (Exception ex)
            {
                _viewModel.SetError($"Failed to initialize browser: {ex.Message}");
            }
        }

        private void InitializeWebView()
        {
            if (WebView.CoreWebView2 == null)
            {
                _viewModel.SetError("WebView2 initialization failed");
                return;
            }

            var webView = WebView.CoreWebView2;

            // Permissions for Teams / Office
            webView.PermissionRequested += (s, e) =>
            {
                e.State = CoreWebView2PermissionState.Allow;
            };

            // External links in system browser
            webView.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                OpenInBrowser(e.Uri);
            };

            // Context menu disabled for app-like feel
            webView.ContextMenuRequested += (s, e) =>
            {
                e.Handled = true;
            };

            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.SourceChanged += WebView_SourceChanged;
            webView.ContentLoading += WebView_ContentLoading;

            _viewModel.UpdateNavigationState(webView);
            _viewModel.ClearError();
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _viewModel.IsLoading = true;
            _viewModel.HasError = false;
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _viewModel.IsLoading = false;
            _viewModel.UpdateNavigationState(WebView.CoreWebView2);

            if (!e.IsSuccess)
            {
                _viewModel.SetError($"Navigation failed: {e.WebErrorStatus}");
            }
        }

        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            _viewModel.UpdateNavigationState(WebView.CoreWebView2);
        }

        private void WebView_ContentLoading(object sender, CoreWebView2ContentLoadingEventArgs e)
        {
            _viewModel.IsLoading = true;
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Optional: handle messages from web content
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CoreWebView2 != null && WebView.CoreWebView2.CanGoBack)
            {
                WebView.CoreWebView2.GoBack();
            }
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CoreWebView2 != null && WebView.CoreWebView2.CanGoForward)
            {
                WebView.CoreWebView2.GoForward();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            WebView.Reload();
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.InitialUrl = _viewModel.Settings.DefaultUrl ?? "https://app.mydesk.digitalresponse.com.au";
            _viewModel.CurrentUrl = _viewModel.InitialUrl;
            WebView.CoreWebView2?.Navigate(_viewModel.InitialUrl);
        }

        private void UrlBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                BtnGo_Click(sender, e);
            }
        }

        private void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            var url = _viewModel.CurrentUrl;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
                _viewModel.CurrentUrl = url;
            }
            WebView.CoreWebView2?.Navigate(url);
        }

        private void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearError();
            WebView.CoreWebView2?.Reload();
        }

        private void BtnOpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_viewModel.CurrentUrl))
            {
                OpenInBrowser(_viewModel.CurrentUrl);
            }
        }

        private void OpenInBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _viewModel.SetError($"Failed to open browser: {ex.Message}");
            }
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            // MyDesk
            var myDeskItem = new MenuItem { Header = "💻 MyDesk", Tag = "mydesk" };
            myDeskItem.Click += AppMenu_Click;
            menu.Items.Add(myDeskItem);

            // Outlook
            var outlookItem = new MenuItem { Header = "📧 Outlook", Tag = "outlook" };
            outlookItem.Click += AppMenu_Click;
            menu.Items.Add(outlookItem);

            // IT Support
            var itItem = new MenuItem { Header = "🛠 IT Support", Tag = "itsupport" };
            itItem.Click += AppMenu_Click;
            menu.Items.Add(itItem);

            // Separator
            menu.Items.Add(new Separator());

            // Show main menu options
            var settingsItem = new MenuItem { Header = "⚙ Settings", Tag = "settings" };
            settingsItem.Click += AppMenu_Click;
            menu.Items.Add(settingsItem);

            var quitItem = new MenuItem { Header = "✖ Quit", Tag = "quit" };
            quitItem.Click += AppMenu_Click;
            menu.Items.Add(quitItem);

            BtnMenu.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private void AppMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string tag)
            {
                switch (tag)
                {
                    case "mydesk":
                        WebView.CoreWebView2?.Navigate("https://app.mydesk.digitalresponse.com.au");
                        break;

                    case "outlook":
                        OpenOutlook();
                        break;

                    case "itsupport":
                        OpenITSupportEmail();
                        break;

                    case "settings":
                        var settingsWindow = new Views.SettingsWindow
                        {
                            Owner = this,
                            DataContext = new ViewModels.SettingsViewModel(null)
                        };
                        settingsWindow.ShowDialog();
                        break;

                    case "quit":
                        Close();
                        break;
                }
            }
        }

        private void OpenOutlook()
        {
            try
            {
                var outlookPath = @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE";
                if (File.Exists(outlookPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = outlookPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Try generic mailto fallback
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "outlook:",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                _viewModel.SetError($"Failed to open Outlook: {ex.Message}");
            }
        }

        private void OpenITSupportEmail()
        {
            try
            {
                var mailto = "mailto:peter@bardenhagen.xyz?subject=IT Support Request";
                Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _viewModel.SetError($"Failed to open email: {ex.Message}");
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Preferred behavior: minimize to tray instead of closing
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.SaveWindowState(this);
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // Could add tray icon behavior here if desired
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Views.SettingsWindow
            {
                Owner = this,
                // Pass null logger; SettingsViewModel loads its own settings
                DataContext = new ViewModels.SettingsViewModel(null)
            };
            settingsWindow.ShowDialog();
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            // Open settings/profile view
            BtnSettings_Click(sender, e);
        }
    }
}