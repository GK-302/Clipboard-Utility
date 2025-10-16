using System;
using System.Globalization;
using System.Windows.Data;

namespace ClipboardUtility.src.Converters
{
    /// <summary>
    /// Inverts a boolean value. Useful for binding IsEnabled to the inverse of a ViewModel boolean.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return System.Windows.Data.Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
