using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClipboardUtility.src.Converters
{
    /// <summary>
    /// ������̒l�Ɋ�Â���Visibility��Ԃ��R���o�[�^�[
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public static readonly StringToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}