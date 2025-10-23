using ClipboardUtility.src.Services;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Reflection;
using System.Linq;

namespace ClipboardUtility.src.Models
{
    /// <summary>
    /// アプリ設定（簡易な JSON ロード／保存を実装）
    /// 将来 UI へ結びつけや DI に置き換えやすい形にしています。
    /// </summary>
    internal sealed class AppSettings
    {
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
        // Whether the application should run at Windows startup
        
        // Whether to use presets for the main operation (if true, a preset is used;
        // otherwise use the simple ProcessingMode selection)
        public bool UsePresets { get; set; } = true;
        // 追加: 保存するカルチャ名 (例: "ja-JP" / "en-US")
        public string CultureName { get; set; } = CultureInfo.CurrentUICulture.Name;

        // タスクトレイアイコン左クリック時に実行するプリセット ID
        public Guid? SelectedPresetId { get; set; } = null;
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("AppSettings {");
            foreach (var prop in typeof(AppSettings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .OrderBy(p => p.Name))
            {
                var value = prop.GetValue(this);
                sb.AppendLine($"  {prop.Name}: {FormatValue(value)}");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string FormatValue(object? v)
        {
            if (v == null) return "<null>";
            if (v is string s) return $"\"{s}\"";
            if (v is double d) return d.ToString("G", CultureInfo.InvariantCulture);
            if (v is float f) return f.ToString("G", CultureInfo.InvariantCulture);
            if (v is IFormattable fmt) return fmt.ToString(null, CultureInfo.InvariantCulture);
            return v.ToString() ?? "<null>";
        }
    }
}
