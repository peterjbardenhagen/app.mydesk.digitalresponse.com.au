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
    /// disabled while an operation is in progress. Also supports binding to Visibility
    /// by returning Collapsed/Visible instead of false/true when targetType is Visibility.
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
            {
                if (targetType == typeof(Visibility))
                    return boolVal ? Visibility.Collapsed : Visibility.Visible;
                return !boolVal;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
                return !boolVal;
            if (value is Visibility vis)
                return vis == Visibility.Collapsed;
            return false;
        }
    }

    /// <summary>
    /// Converts a radio button's IsChecked bound to a string-parameter comparison.
    /// When the bound property matches ConverterParameter, IsChecked = true.
    /// Use with GroupName on the RadioButton for mutual exclusivity.
    /// </summary>
    public class RadioBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strVal && parameter is string param)
                return string.Equals(strVal, param, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string param)
                return param;
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Shows an element when the string value equals the ConverterParameter.
    /// </summary>
    public class StringEqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strVal && parameter is string param)
                return string.Equals(strVal, param, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
