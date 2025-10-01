using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using ClipboardUtility.src.Properties;
using ClipboardUtility.src.Services;

namespace ClipboardUtility.src.Converters
{
    [ValueConversion(typeof(ProcessingMode), typeof(string))]
    public class ProcessingModeToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProcessingMode mode)
            {
                try
                {
                    var fi = mode.GetType().GetField(mode.ToString());
                    var attr = fi?.GetCustomAttribute<ResourceKeyAttribute>();
                    string primaryKey = attr?.Key ?? $"NotificationFormat_{mode}";

                    string? display = Resources.ResourceManager.GetString(primaryKey, culture ?? CultureInfo.CurrentUICulture);
                    if (!string.IsNullOrEmpty(display))
                    {
                        return display;
                    }
                }
                catch
                {
                    // ignore and fallback
                }

                return mode.ToString();
            }

            return System.Windows.Data.Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
