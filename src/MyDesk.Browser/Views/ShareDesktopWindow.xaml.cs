using System.Windows;
using MyDesk.Browser.ViewModels;

namespace MyDesk.Browser.Views
{
    public partial class ShareDesktopWindow : Window
    {
        public ShareDesktopWindow()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
