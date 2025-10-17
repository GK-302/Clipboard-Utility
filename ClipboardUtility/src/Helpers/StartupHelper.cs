using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace ClipboardUtility.src.Helpers
{
    internal static class StartupHelper
    {
        private const string RunKeyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        public static void SetRunAtStartup(bool enable)
        {
            try
            {
                var productName = GetProductName();
                if (string.IsNullOrEmpty(productName)) return;

                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return;

                if (enable)
                {
                    // Prefer to reference a shortcut in Start Menu Programs if available (ClickOnce style)
                    var publisherName = GetPublisherName();
                    var startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    var shortcutPath = Path.Combine(startupFolderPath, publisherName, productName + ".appref-ms");

                    if (File.Exists(shortcutPath))
                    {
                        key.SetValue(productName, '"' + shortcutPath + '"');
                    }
                    else
                    {
                        // Fallback to exe path
                        var exePath = Assembly.GetEntryAssembly()?.Location;
                        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        {
                            key.SetValue(productName, '"' + exePath + '"');
                        }
                    }
                }
                else
                {
                    if (key.GetValue(productName) != null)
                        key.DeleteValue(productName);
                }
            }
            catch (Exception ex)
            {
                // Log and rethrow to allow callers to handle as needed
                FileLogger.LogException(ex, "StartupHelper.SetRunAtStartup");
                throw;
            }
        }

        private static string GetProductName()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return string.Empty;

            var productAttribute = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            return productAttribute?.Product ?? entryAssembly.GetName().Name ?? "ClipboardUtility";
        }

        private static string GetPublisherName()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return string.Empty;

            var companyAttribute = entryAssembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            return companyAttribute?.Company ?? GetProductName();
        }
    }
}
