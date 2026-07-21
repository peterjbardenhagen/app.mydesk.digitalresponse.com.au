using System.Windows;
using MyDesk.Browser.ViewModels;

namespace MyDesk.Browser.Views
{
    public partial class SupportWindow : Window
    {
        public SupportWindow(string currentUser)
        {
            InitializeComponent();
            DataContext = new SupportViewModel(currentUser);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
