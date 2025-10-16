using ClipboardUtility.src.Services;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipboardUtility.src.Models
{
    /// <summary>
    /// アプリ設定（簡易な JSON ロード／保存を実装）
    /// 将来 UI へ結びつけや DI に置き換えやすい形にしています。
    /// </summary>
    internal sealed class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config/appsettings.json");

        // 既存の設定
        public ProcessingMode ClipboardProcessingMode { get; set; } = ProcessingMode.NormalizeWhitespace;

        // Notification の表示オフセット（ピクセル、スクリーン物理ピクセル単位）
        public int NotificationOffsetX { get; set; } = 20;
        public int NotificationOffsetY { get; set; } = 20;

        // Notification の余白（スクリーン物理ピクセル単位）
        public int NotificationMargin { get; set; } = 0;

        // NotificationWindow サイズ制約（WPF device-independent units）
        public double NotificationMinWidth { get; set; } = 160.0;
        public double NotificationMaxWidth { get; set; } = 420.0;
        public double NotificationMinHeight { get; set; } = 48.0;
        public double NotificationMaxHeight { get; set; } = 400.0;
        public int NotificationDelay { get; set; } = 100; // ミリ秒
        public bool ShowCopyNotification { get; set; } = true;
        public bool ShowOperationNotification { get; set; } = true;
        public bool ShowWelcomeNotification { get; set; } = true;
        
        // Whether to use presets for the main operation (if true, a preset is used;
        // otherwise use the simple ProcessingMode selection)
        public bool UsePresets { get; set; } = true;
        // 追加: 保存するカルチャ名 (例: "ja-JP" / "en-US")
        public string CultureName { get; set; } = CultureInfo.CurrentUICulture.Name;

        // タスクトレイアイコン左クリック時に実行するプリセット ID
        public Guid? SelectedPresetId { get; set; } = null;

        // 将来の拡張用
        public static AppSettings Load()
        {
            try
            {
                Debug.WriteLine($"AppSettings: Load() called. Searching for settings file at: '{SettingsFilePath}'");

                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    Debug.WriteLine($"AppSettings: Found settings file. Length={json?.Length ?? 0} bytes");

                    var opts = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    // enum を文字列として扱うコンバータ
                    opts.Converters.Add(new JsonStringEnumConverter());

                    var settings = JsonSerializer.Deserialize<AppSettings>(json, opts);

                    if (settings != null)
                    {
                        Debug.WriteLine($"AppSettings: Successfully deserialized settings from '{SettingsFilePath}'");
                        Debug.WriteLine($"AppSettings: ClipboardProcessingMode={settings.ClipboardProcessingMode}, NotificationOffsetX={settings.NotificationOffsetX}, NotificationOffsetY={settings.NotificationOffsetY}, NotificationMargin={settings.NotificationMargin}, MinW={settings.NotificationMinWidth}, MaxW={settings.NotificationMaxWidth}, MinH={settings.NotificationMinHeight}, MaxH={settings.NotificationMaxHeight}");
                        return settings;
                    }
                    else
                    {
                        Debug.WriteLine("AppSettings: Deserialization returned null; falling back to defaults.");
                    }
                }
                else
                {
                    Debug.WriteLine($"AppSettings: Settings file not found at '{SettingsFilePath}'. Using defaults.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AppSettings: failed to load settings from '{SettingsFilePath}': {ex}");
            }

            // フォールバック：既定値を返す
            Debug.WriteLine("AppSettings: Returning default settings instance.");
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var opts = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                // 保存時も enum を文字列として出力
                opts.Converters.Add(new JsonStringEnumConverter());

                var json = JsonSerializer.Serialize(this, opts);
                File.WriteAllText(SettingsFilePath, json);
                Debug.WriteLine($"AppSettings: Saved settings to '{SettingsFilePath}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AppSettings: failed to save settings: {ex.Message}");
            }
        }
    }
}
