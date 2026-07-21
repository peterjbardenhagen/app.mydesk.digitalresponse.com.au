using System.Windows;
using MyDesk.Browser.ViewModels;

namespace MyDesk.Browser.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;

            // Populate UI
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Show WebView2 user data folder
            var userDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyDesk",
                "Browser",
                "WebView2");
            UserDataFolderText.Text = userDataFolder;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}