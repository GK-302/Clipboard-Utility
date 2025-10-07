using ClipboardUtility.src.Properties;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace ClipboardUtility.src.Services
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class ResourceKeyAttribute : Attribute
    {
        public string Key { get; }
        public ResourceKeyAttribute(string key) => Key = key;
    }

    internal static class ProcessingModeExtensions
    {
        /// <summary>
        /// ProcessingMode に紐づく通知メッセージを Resources から取得して返す。
        /// 見つからない場合はモード毎のフォールバックキー → リテラルの順で安全に返します。
        /// </summary>
        public static string GetNotificationMessage(this ProcessingMode mode, params object[] formatArgs)
        {
            try
            {
                var fi = mode.GetType().GetField(mode.ToString());
                var attr = fi?.GetCustomAttribute<ResourceKeyAttribute>();

                // まず属性または規約ベースのキーを試す
                string primaryKey = attr?.Key ?? $"NotificationFormat_{mode}";
                var culture = CultureInfo.CurrentUICulture;
                string? messageTemplate = Resources.ResourceManager.GetString(primaryKey, culture);

                // モード毎のフォールバック用キー（属性や規約キーがない／未定義のときに使う）
                var fallbackKeyMap = new Dictionary<ProcessingMode, string>
                {
                    { ProcessingMode.RemoveLineBreaks, "NotificationFormat_LineBreakRemoved" },
                    { ProcessingMode.NormalizeWhitespace, "NotificationFormat_WhitespaceNormalized" },
                    { ProcessingMode.NormalizeUnicode, "NotificationFormat_NormalizeUnicode" },
                    { ProcessingMode.RemoveDiacritics, "NotificationFormat_RemoveDiacritics" },
                    { ProcessingMode.RemovePunctuation, "NotificationFormat_RemovePunctuation" },
                    { ProcessingMode.RemoveControlCharacters, "NotificationFormat_RemoveControlChars" },
                    { ProcessingMode.RemoveUrls, "NotificationFormat_RemoveUrls" },
                    { ProcessingMode.RemoveEmails, "NotificationFormat_RemoveEmails" },
                    { ProcessingMode.RemoveHtmlTags, "NotificationFormat_RemoveHtmlTags" },
                    { ProcessingMode.StripMarkdownLinks, "NotificationFormat_StripMarkdownLinks" },
                    { ProcessingMode.ConvertTabsToSpaces, "NotificationFormat_ConvertTabsToSpaces" },
                    { ProcessingMode.Trim, "NotificationFormat_Trim" },
                    { ProcessingMode.ToUpper, "NotificationFormat_ToUpper" },
                    { ProcessingMode.ToLower, "NotificationFormat_ToLower" },
                    { ProcessingMode.ToTitleCase, "NotificationFormat_ToTitleCase" },
                    { ProcessingMode.ToPascalCase, "NotificationFormat_ToPascalCase" },
                    { ProcessingMode.ToCamelCase, "NotificationFormat_ToCamelCase" },
                    { ProcessingMode.Truncate, "NotificationFormat_Truncate" },
                    { ProcessingMode.JoinLinesWithSpace, "NotificationFormat_JoinLinesWithSpace" },
                    { ProcessingMode.RemoveDuplicateLines, "NotificationFormat_RemoveDuplicateLines" },
                    { ProcessingMode.CollapseWhitespace, "NotificationFormat_CollapseWhitespace" }
                };

                // フォールバックキーで再取得
                if (string.IsNullOrEmpty(messageTemplate) && fallbackKeyMap.TryGetValue(mode, out var fallbackKey))
                {
                    messageTemplate = Resources.ResourceManager.GetString(fallbackKey, culture);
                }

                // 最終フォールバック（各モードに合った安全なリテラル）
                if (string.IsNullOrEmpty(messageTemplate))
                {
                    Debug.WriteLine($"Resource not found for mode '{mode}' (primaryKey='{primaryKey}'). Using literal fallback.");
                    messageTemplate = mode switch
                    {
                        ProcessingMode.RemoveLineBreaks => "Line breaks removed",
                        ProcessingMode.NormalizeWhitespace => "Whitespace normalized",
                        ProcessingMode.NormalizeUnicode => "Text normalized",
                        ProcessingMode.RemoveDiacritics => "Diacritics removed",
                        ProcessingMode.RemovePunctuation => "Punctuation removed",
                        ProcessingMode.RemoveControlCharacters => "Control characters removed",
                        ProcessingMode.RemoveUrls => "URLs removed",
                        ProcessingMode.RemoveEmails => "Emails removed",
                        ProcessingMode.RemoveHtmlTags => "HTML tags removed",
                        ProcessingMode.StripMarkdownLinks => "Markdown links stripped",
                        ProcessingMode.ConvertTabsToSpaces => "Tabs converted to spaces",
                        ProcessingMode.Trim => "Trimmed whitespace",
                        ProcessingMode.ToUpper => "Converted to upper case",
                        ProcessingMode.ToLower => "Converted to lower case",
                        ProcessingMode.ToTitleCase => "Converted to title case",
                        ProcessingMode.ToPascalCase => "Converted to PascalCase",
                        ProcessingMode.ToCamelCase => "Converted to camelCase",
                        ProcessingMode.Truncate => "Text truncated",
                        ProcessingMode.JoinLinesWithSpace => "Lines joined with spaces",
                        ProcessingMode.RemoveDuplicateLines => "Duplicate lines removed",
                        ProcessingMode.CollapseWhitespace => "Whitespace collapsed",
                        _ => "Clipboard operation completed"
                    };
                }

                if (formatArgs != null && formatArgs.Length > 0)
                {
                    return string.Format(CultureInfo.CurrentCulture, messageTemplate, formatArgs);
                }

                return messageTemplate;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resolving notification message for mode {mode}: {ex.Message}");
                return "Clipboard operation completed";
            }
        }
    }
}