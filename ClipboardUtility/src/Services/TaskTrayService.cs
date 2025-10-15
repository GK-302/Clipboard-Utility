using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace ClipboardUtility.src.Services;

public class TaskTrayService : ITaskTrayService, IDisposable
{
    #region Singleton実装

    // 1. 静的なインスタンス変数（初期値はnull）
    private static TaskTrayService _instance;

    // 2. スレッドセーフのためのロックオブジェクト
    private static readonly object _lock = new();

    // 3. 外部からインスタンスにアクセスするためのプロパティ
    public static TaskTrayService Instance
    {
        get
        {
            // Double-checked lockingパターン
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new TaskTrayService();
                    }
                }
            }
            return _instance;
        }
    }

    // 4. プライベートコンストラクタ（外部からnewできないようにする）
    private TaskTrayService()
    {
        // 初期化処理は必要に応じてここに
    }

    #endregion

    private NotifyIcon _notifyIcon;
    private bool _isInitialized = false;
    private src.Views.ClipboardManagerWindow _clipboardManagerWindow;
    private PresetService _presetService;
    private TextProcessingService _textProcessingService;
    private ClipboardService _clipboardService;

    // クリップボードの統計情報を保持
    private string _currentClipboardText = string.Empty;

    // イベントを定義
    public event EventHandler ClipboardOperationRequested;
    public event EventHandler ShowWindowRequested;
    public event EventHandler ExitApplicationRequested;
    public event EventHandler<ProcessingPreset> PresetExecutionRequested;

    public void Initialize()
    {
        // 既に初期化済みの場合は何もしない
        if (_isInitialized)
        {
            Debug.WriteLine("TaskTrayService is already initialized.");
            return;
        }

        // サービスの初期化
        _textProcessingService = new TextProcessingService();
        _presetService = new PresetService(_textProcessingService);
        _presetService.LoadPresets();
        _clipboardService = new ClipboardService();

        System.IO.Stream iconStream;
        try
        {
            var iconURI = new Uri("pack://application:,,,/src/Assets/drawing_1.ico");
            iconStream = System.Windows.Application.GetResourceStream(iconURI).Stream;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("failed to load icon file: " + ex.Message);
            return;
        }

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Icon = new Icon(iconStream),
            Text = "Clipboard Utility"
        };

        _notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(OnNotifyIconClicked);
        _isInitialized = true;

        // 初期ツールチップを設定
        UpdateTooltip(string.Empty);

        Debug.WriteLine("TaskTrayService initialized successfully.");
    }

    /// <summary>
    /// クリップボードのテキストが更新されたときに呼び出されます
    /// </summary>
    public void UpdateClipboardInfo(string clipboardText)
    {
        _currentClipboardText = clipboardText ?? string.Empty;
        UpdateTooltip(_currentClipboardText);
    }

    /// <summary>
    /// ツールチップを更新します
    /// </summary>
    private void UpdateTooltip(string text)
    {
        if (_notifyIcon == null || !_isInitialized)
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(text))
            {
                _notifyIcon.Text = "Clipboard Utility\n" + LocalizedStrings.Instance.NoClipboardDataText;
            }
            else
            {
                int charCount = text.Length;
                int wordCount = CountWords(text);
                int lineCount = CountLines(text);

                string tooltip = $"Clipboard Utility\n" +
                                $"{LocalizedStrings.Instance.CharacterCountText}: {charCount}\n" +
                                $"{LocalizedStrings.Instance.WordCountText}: {wordCount}\n" +
                                $"{LocalizedStrings.Instance.LineCountText}: {lineCount}";

                // NotifyIconのTextプロパティは最大63文字の制限があります
                if (tooltip.Length > 63)
                {
                    tooltip = tooltip.Substring(0, 60) + "...";
                }

                _notifyIcon.Text = tooltip;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TaskTrayService: UpdateTooltip failed: {ex}");
        }
    }

    /// <summary>
    /// テキスト内の単語数をカウントします
    /// </summary>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // 空白文字で分割して単語数をカウント
        var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    /// <summary>
    /// テキスト内の行数をカウントします
    /// </summary>
    private int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // 改行で分割して行数をカウント
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        // 空行も含めるが、末尾の余分な改行は除外
        int count = lines.Length;
        if (count > 0 && string.IsNullOrEmpty(lines[count - 1]))
        {
            count--;
        }
        return Math.Max(1, count);
    }

    private void OnNotifyIconClicked(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // 左クリック: 設定で選択されたプリセットを実行
            ExecuteSelectedPreset();
        }
        else if (e.Button == MouseButtons.Right)
        {
            // 右クリック: クリップボードマネージャーウィンドウを表示
            ShowClipboardManager();
        }
    }

    /// <summary>
    /// 設定で選択されたプリセットを実行します
    /// </summary>
    private void ExecuteSelectedPreset()
    {
        try
        {
            var settings = SettingsService.Instance.Current;
            if (!settings.SelectedPresetId.HasValue)
            {
                Debug.WriteLine("TaskTrayService.ExecuteSelectedPreset: No preset selected in settings");
                ClipboardOperationRequested?.Invoke(this, EventArgs.Empty); // フォールバック：既存のイベント
                return;
            }

            var preset = _presetService.GetPresetById(settings.SelectedPresetId.Value);
            if (preset == null)
            {
                Debug.WriteLine($"TaskTrayService.ExecuteSelectedPreset: Preset not found: {settings.SelectedPresetId.Value}");
                ClipboardOperationRequested?.Invoke(this, EventArgs.Empty); // フォールバック：既存のイベント
                return;
            }

            var clipboardText = _clipboardService.GetTextSafely();
            if (string.IsNullOrEmpty(clipboardText))
            {
                Debug.WriteLine("TaskTrayService.ExecuteSelectedPreset: No text in clipboard");
                return;
            }

            var result = _presetService.ExecutePreset(preset, clipboardText);
            _clipboardService.SetText(result);

            Debug.WriteLine($"TaskTrayService.ExecuteSelectedPreset: Executed preset '{preset.Name}'");
            
            // イベントを発火（通知表示用）
            PresetExecutionRequested?.Invoke(this, preset);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TaskTrayService.ExecuteSelectedPreset: Failed: {ex.Message}");
            FileLogger.LogException(ex, "TaskTrayService.ExecuteSelectedPreset");
        }
    }

    /// <summary>
    /// クリップボードマネージャーウィンドウを表示します
    /// </summary>
    private void ShowClipboardManager()
    {
        try
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                // 既にウィンドウが開いている場合は、それをアクティブにする
                if (_clipboardManagerWindow != null && _clipboardManagerWindow.IsLoaded)
                {
                    if (_clipboardManagerWindow.WindowState == WindowState.Minimized)
                    {
                        _clipboardManagerWindow.WindowState = WindowState.Normal;
                    }
                    _clipboardManagerWindow.Activate();
                }
                else
                {
                    // 新しいウィンドウを作成
                    _clipboardManagerWindow = new src.Views.ClipboardManagerWindow();
                    _clipboardManagerWindow.Closed += (s, e) => _clipboardManagerWindow = null;
                    _clipboardManagerWindow.Show();
                    _clipboardManagerWindow.Activate();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TaskTrayService: Failed to show clipboard manager: {ex.Message}");
            FileLogger.LogException(ex, "TaskTrayService.ShowClipboardManager");
        }
    }

    private void OnShowClicked(object sender, EventArgs e)
    {
        // イベントを発火
        ShowWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        // イベントを発火
        ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_clipboardManagerWindow != null)
        {
            _clipboardManagerWindow.Close();
            _clipboardManagerWindow = null;
        }
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        _isInitialized = false;

        // Singletonインスタンスもリセット
        lock (_lock)
        {
            _instance = null;
        }
    }
}
