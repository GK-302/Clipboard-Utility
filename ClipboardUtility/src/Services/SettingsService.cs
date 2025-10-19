using ClipboardUtility.src.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace ClipboardUtility.src.Services;

// シンプルなアプリ設定読み書きと変更通知のシングルトンサービス
internal sealed class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    internal static SettingsService Instance => _instance.Value;

    private readonly string _projectConfigPath;
    private readonly string _appDataDirectory;
    private readonly string _appDataConfigPath;

    public AppSettings Current { get; private set; } = new AppSettings();

    public event EventHandler<AppSettings>? SettingsChanged;

    private SettingsService()
    {
        _projectConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json");

        // アプリ毎にフォルダを作る（Assembly の Product/Name を利用）
        var productFolder = GetProductFolderName();
        _appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), productFolder);
        _appDataConfigPath = Path.Combine(_appDataDirectory, "config/appsettings.json");

        Load();
    }

    public void Load()
    {
        Debug.WriteLine("SettingsService.Load: start");

        // 1) まず AppData を優先
        if (TryLoadFromPath(_appDataConfigPath, out var loadedFromAppData))
        {
            Current = loadedFromAppData!;
            Debug.WriteLine($"SettingsService.Load: loaded settings from AppData '{_appDataConfigPath}'");
            NotifySettingsChanged();
            return;
        }

        // 2) AppDataに無ければプロジェクト（exe 配下）のデフォルトを試す
        if (TryLoadFromPath(_projectConfigPath, out var loadedFromProject))
        {
            Current = loadedFromProject!;
            Debug.WriteLine($"SettingsService.Load: loaded settings from default '{_projectConfigPath}'");

            // デフォルトが見つかったら AppData にコピーして以後は AppData を使う
            try
            {
                Directory.CreateDirectory(_appDataDirectory);
                var opts = new JsonSerializerOptions { WriteIndented = true };
                opts.Converters.Add(new JsonStringEnumConverter());
                var json = JsonSerializer.Serialize(Current, opts);
                File.WriteAllText(_appDataConfigPath, json);
                Debug.WriteLine($"SettingsService.Load: copied default config to AppData '{_appDataConfigPath}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsService.Load: failed to copy default to AppData: {ex}");
            }

            NotifySettingsChanged();
            return;
        }

        // 3) どこにも無ければ既定値を使い、AppData に書き出す（作成）
        Current = new AppSettings();
        Debug.WriteLine("SettingsService.Load: no config found, using defaults.");
        try
        {
            Directory.CreateDirectory(_appDataDirectory);
            var opts = new JsonSerializerOptions { WriteIndented = true };
            opts.Converters.Add(new JsonStringEnumConverter());
            var json = JsonSerializer.Serialize(Current, opts);
            File.WriteAllText(_appDataConfigPath, json);
            Debug.WriteLine($"SettingsService.Load: wrote default config to AppData '{_appDataConfigPath}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsService.Load: failed to write default config to AppData: {ex}");
        }

        NotifySettingsChanged();
    }

    private bool TryLoadFromPath(string path, out AppSettings? settings)
    {
        settings = null;
        try
        {
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            Debug.WriteLine($"SettingsService.TryLoadFromPath: read {path} ({json?.Length ?? 0} bytes)");
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            opts.Converters.Add(new JsonStringEnumConverter());
            settings = JsonSerializer.Deserialize<AppSettings>(json, opts) ?? new AppSettings();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsService.TryLoadFromPath: failed to read/deserialize '{path}': {ex}");
            return false;
        }
    }

    public void Save(AppSettings settings)
    {
        Debug.WriteLine("SettingsService.Save: called");

        if (settings == null) settings = new AppSettings();

        // Current を受け取ったコピーで置き換える（呼び出し側は GetSettingsCopy() が渡される想定）
        Current = settings;

        try
        {
            var dir = Path.GetDirectoryName(_appDataConfigPath);
            if (!string.IsNullOrEmpty(dir)) _ = Directory.CreateDirectory(dir);

            var opts = new JsonSerializerOptions { WriteIndented = true };
            opts.Converters.Add(new JsonStringEnumConverter());
            var json = JsonSerializer.Serialize(Current, opts);
            File.WriteAllText(_appDataConfigPath, json);
            Debug.WriteLine($"SettingsService.Save: wrote settings to '{_appDataConfigPath}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsService.Save: failed to write runtime settings: {ex}");
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

    private static string GetProductFolderName()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return "ClipboardUtility";
            var productAttribute = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            return productAttribute?.Product ?? entryAssembly.GetName().Name ?? "ClipboardUtility";
        }
        catch
        {
            return "ClipboardUtility";
        }
    }
}