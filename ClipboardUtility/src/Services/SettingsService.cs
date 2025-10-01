using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using ClipboardUtility.src.Models;

namespace ClipboardUtility.src.Services;

// シンプルなアプリ設定読み書きと変更通知のシングルトンサービス
internal sealed class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    internal static SettingsService Instance => _instance.Value;

    private readonly string _path;
    public AppSettings Current { get; private set; } = new AppSettings();

    public event EventHandler<AppSettings>? SettingsChanged;

    private SettingsService()
    {
        _path = Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                Debug.WriteLine($"SettingsService.Load: raw JSON ({json.Length} bytes): {json}");

                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                opts.Converters.Add(new JsonStringEnumConverter());

                Current = JsonSerializer.Deserialize<AppSettings>(json, opts) ?? new AppSettings();
                Debug.WriteLine($"SettingsService.Load: loaded settings from {_path}");
            }
            else
            {
                Current = new AppSettings();
                Debug.WriteLine($"SettingsService.Load: settings file not found; using defaults.");
            }
        }
        catch (Exception ex)
        {
            Current = new AppSettings();
            Debug.WriteLine($"SettingsService.Load: failed: {ex}");
        }

        // 読み込み後に購読者へ通知（UIスレッドで）
        Debug.WriteLine("SettingsService.Load: calling NotifySettingsChanged()");
        NotifySettingsChanged();
    }

    // Save / NotifySettingsChanged は既存コードのまま
    public void Save(AppSettings settings)
    {
        Debug.WriteLine("SettingsService.Save: called");
        var newSettings = settings ?? new AppSettings();
        Current = new AppSettings
        {
            ClipboardProcessingMode = newSettings.ClipboardProcessingMode,
            NotificationOffsetX = newSettings.NotificationOffsetX,
            NotificationOffsetY = newSettings.NotificationOffsetY,
            NotificationMargin = newSettings.NotificationMargin,
            NotificationMinWidth = newSettings.NotificationMinWidth,
            NotificationMaxWidth = newSettings.NotificationMaxWidth,
            NotificationMinHeight = newSettings.NotificationMinHeight,
            NotificationMaxHeight = newSettings.NotificationMaxHeight
        };

        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var opts = new JsonSerializerOptions { WriteIndented = true };
            opts.Converters.Add(new JsonStringEnumConverter());
            var json = JsonSerializer.Serialize(Current, opts);
            File.WriteAllText(_path, json);
            Debug.WriteLine($"SettingsService.Save: wrote settings to {_path}");

            try
            {
                var maxPreview = 32 * 1024;
                if (json.Length <= maxPreview)
                {
                    Debug.WriteLine($"SettingsService.Save: serialized JSON ({json.Length} bytes):\n{json}");
                }
                else
                {
                    Debug.WriteLine($"SettingsService.Save: serialized JSON ({json.Length} bytes). Dumping first {maxPreview} chars:\n{json.Substring(0, maxPreview)}\n... (truncated)");
                }
            }
            catch (Exception dbgEx)
            {
                Debug.WriteLine($"SettingsService.Save: failed to write dcug JSON output: {dbgEx}");
            }

            try
            {
                if (Debugger.IsAttached)
                {
                    var projectCopy = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "config", "appsettings.json"));
                    var projectDir = Path.GetDirectoryName(projectCopy);
                    if (!string.IsNullOrEmpty(projectDir)) Directory.CreateDirectory(projectDir);
                    File.WriteAllText(projectCopy, json);
                    Debug.WriteLine($"SettingsService.Save: also wrote project copy to {projectCopy}");
                }
            }
            catch (Exception copyEx)
            {
                Debug.WriteLine($"SettingsService.Save: failed to write project copy: {copyEx}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsService.Save: failed to write settings: {ex}");
            // 必要ならログを出す
        }

        // 保存後は必ず購読者へ通知（UI スレッドで実行）
        Debug.WriteLine("SettingsService.Save: calling NotifySettingsChanged()");
        NotifySettingsChanged();
    }

    private void NotifySettingsChanged()
    {
        var handler = SettingsChanged;
        if (handler == null)
        {
            Debug.WriteLine("SettingsService.NotifySettingsChanged: no subscribers");
            return;
        }

        Debug.WriteLine("SettingsService.NotifySettingsChanged: invoking subscribers");
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher != null)
        {
            app.Dispatcher.Invoke(() => handler(this, Current));
        }
        else
        {
            handler(this, Current);
        }
    }
}