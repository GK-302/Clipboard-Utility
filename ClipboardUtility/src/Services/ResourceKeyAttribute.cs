using System;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;
using ClipboardUtility.src.Properties;

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
        public static string GetNotificationMessage(this ProcessingMode mode, params object[] formatArgs)
        {
            try
            {
                var fi = mode.GetType().GetField(mode.ToString());
                var attr = fi?.GetCustomAttribute<ResourceKeyAttribute>();
                string key = attr?.Key ?? $"NotificationFormat_{mode}";

                // まずリソースから取得（CurrentUICulture）
                string messageTemplate = Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);

                if (string.IsNullOrEmpty(messageTemplate))
                {
                    Debug.WriteLine($"Resource not found for key '{key}'. Using fallback.");
                    // モードごとに適切なフォールバックを用意（誤ったキー参照を避ける）
                    messageTemplate = mode switch
                    {
                        ProcessingMode.RemoveLineBreaks => Resources.ResourceManager.GetString("NotificationFormat_LineBreakRemoved", CultureInfo.CurrentUICulture)
                                                            ?? "Line breaks removed",
                        ProcessingMode.NormalizeWhitespace => Resources.ResourceManager.GetString("NotificationFormat_WhitespaceNormalized", CultureInfo.CurrentUICulture)
                                                            ?? "Whitespace normalized",
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