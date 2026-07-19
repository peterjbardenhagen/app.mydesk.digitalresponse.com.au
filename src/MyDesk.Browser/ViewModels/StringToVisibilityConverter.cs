using System;
using System.Globalization;
using System.Windows.Data;

namespace MyDesk.Browser.ViewModels
{
    /// <summary>
    /// Converts a string to Visibility - Visible if string is not null or empty, Collapsed otherwise
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}