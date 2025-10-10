using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Properties;
using ClipboardUtility.src.Services;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace ClipboardUtility.src.Converters
{
    /// <summary>
    /// ProcessingModeを多言語対応の表示文字列に変換するコンバーター
    /// LocalizedStringsの変更通知を受け取って自動更新に対応
    /// </summary>
    [ValueConversion(typeof(ProcessingMode), typeof(string))]
    public class ProcessingModeToDisplayConverter : IValueConverter, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ProcessingModeToDisplayConverter()
        {
            // LocalizedStringsの変更通知を購読して、言語変更時に再変換をトリガー
            LocalizedStrings.Instance.PropertyChanged += OnLocalizedStringsChanged;
        }

        private void OnLocalizedStringsChanged(object sender, PropertyChangedEventArgs e)
        {
            // いずれかのリソースが変更された場合、このコンバーターも更新が必要
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProcessingMode mode)
            {
                try
                {
                    // まずコンボ表示用のリソースキーを優先（ProcessingModeDisplay_{mode}）
                    string displayKey = $"ProcessingModeDisplay_{mode}";
                    string? display = Resources.ResourceManager.GetString(displayKey, culture ?? CultureInfo.CurrentUICulture);

                    if (!string.IsNullOrEmpty(display))
                    {
                        return display;
                    }

                    // 表示用リソースが見つからない場合は短いフォールバック名を返す
                    return GetFallbackDisplayName(mode);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ProcessingModeToDisplayConverter: Error converting {mode}: {ex.Message}");
                    return GetFallbackDisplayName(mode);
                }
            }

            return System.Windows.Data.Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ProcessingModeToDisplayConverter does not support ConvertBack");
        }

        /// <summary>
        /// リソースが見つからない場合のフォールバック表示名（短いラベル）
        /// </summary>
        private string GetFallbackDisplayName(ProcessingMode mode)
        {
            return mode switch
            {
                ProcessingMode.None => "No Processing",
                ProcessingMode.RemoveLineBreaks => "Remove Line Breaks",
                ProcessingMode.NormalizeWhitespace => "Normalize Whitespace",
                ProcessingMode.NormalizeUnicode => "Normalize Unicode",
                ProcessingMode.RemoveDiacritics => "Remove Diacritics",
                ProcessingMode.RemovePunctuation => "Remove Punctuation",
                ProcessingMode.RemoveControlCharacters => "Remove Control Characters",
                ProcessingMode.RemoveUrls => "Remove URLs",
                ProcessingMode.RemoveEmails => "Remove Emails",
                ProcessingMode.RemoveHtmlTags => "Remove HTML Tags",
                ProcessingMode.StripMarkdownLinks => "Strip Markdown Links",
                ProcessingMode.ConvertTabsToSpaces => "Convert Tabs to Spaces",
                ProcessingMode.Trim => "Trim Whitespace",
                ProcessingMode.ToUpper => "Convert to Upper Case",
                ProcessingMode.ToLower => "Convert to Lower Case",
                ProcessingMode.ToTitleCase => "Convert to Title Case",
                ProcessingMode.ToPascalCase => "Convert to PascalCase",
                ProcessingMode.ToCamelCase => "Convert to camelCase",
                ProcessingMode.Truncate => "Truncate Text",
                ProcessingMode.JoinLinesWithSpace => "Join Lines with Space",
                ProcessingMode.RemoveDuplicateLines => "Remove Duplicate Lines",
                ProcessingMode.CollapseWhitespace => "Collapse Whitespace",
                _ => mode.ToString()
            };
        }
    }
}
