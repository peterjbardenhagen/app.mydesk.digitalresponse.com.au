using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyDesk.Browser.ViewModels
{
    /// <summary>
    /// Converts a string to Visibility — Visible if not null/empty, Collapsed otherwise.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts a boolean value. Used for binding IsEnabled when a command should be
    /// disabled while an operation is in progress.
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
                return !boolVal;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
                return !boolVal;
            return false;
        }
    }
}
