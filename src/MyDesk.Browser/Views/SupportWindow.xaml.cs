using System.Windows;
using MyDesk.Browser.ViewModels;

namespace MyDesk.Browser.Views
{
    public partial class SupportWindow : Window
    {
        public SupportWindow(string currentUser, string supportEmail = "peter@bardenhagen.xyz")
        {
            InitializeComponent();
            DataContext = new SupportViewModel(currentUser, supportEmail);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}