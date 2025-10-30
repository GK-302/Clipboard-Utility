using ClipboardUtility.src.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace ClipboardUtility.src.Converters;

public class PresetToBooleanConverter : IValueConverter
{
    // ViewModel (Enum) -> RadioButton (bool)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PresetType presetType && parameter is PresetType targetPreset)
        {
            return presetType == targetPreset;
        }
        return false;
    }

    // RadioButton (bool) -> ViewModel (Enum)
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is PresetType targetPreset)
        {
            return targetPreset;
        }
        return Binding.DoNothing; // チェックが外れた場合は何もしない
    }
}