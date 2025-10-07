using ClipboardUtility.src.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        Debug.WriteLine("SettingsService.Load: calling NotifySettingsChanged()");
        NotifySettingsChanged();
    }

    public void Save(AppSettings settings)
    {
        Debug.WriteLine("SettingsService.Save: called");

        if (settings == null) settings = new AppSettings();

        // Current を受け取ったコピーで置き換える（呼び出し側は GetSettingsCopy() が渡される想定）
        Current = settings;

        string json = string.Empty;

        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) _ = Directory.CreateDirectory(dir);

            var opts = new JsonSerializerOptions { WriteIndented = true };
            opts.Converters.Add(new JsonStringEnumConverter());
            json = JsonSerializer.Serialize(Current, opts);
            File.WriteAllText(_path, json);
            Debug.WriteLine($"SettingsService.Save: wrote settings to {_path}");
            Debug.WriteLine($"SettingsService.Save: serialized JSON ({json.Length} bytes):\n{json}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsService.Save: failed to write runtime settings: {ex}");
        }

        // デバッグモードで実行時のみ、プロジェクト直下の ../../config/appsettings.json を更新する
        if (Debugger.IsAttached)
        {
            try
            {
                var projectCopy = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "config", "appsettings.json"));
                var projectDir = Path.GetDirectoryName(projectCopy);
                if (!string.IsNullOrEmpty(projectDir)) _ = Directory.CreateDirectory(projectDir);
                File.WriteAllText(projectCopy, json);
                Debug.WriteLine($"SettingsService.Save: also wrote project copy to {projectCopy}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsService.Save: failed to write project copy: {ex}");
            }
        }
        else
        {
            Debug.WriteLine("SettingsService.Save: not in debug mode; skipping project copy.");
        }

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