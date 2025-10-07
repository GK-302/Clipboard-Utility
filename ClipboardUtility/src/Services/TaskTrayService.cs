using ClipboardUtility.src.Helpers;
using System.ComponentModel;
using System.Diagnostics;


namespace ClipboardUtility.src.Services;

public class TaskTrayService : ITaskTrayService, IDisposable
{
    #region Singleton実装

    // 1. 静的なインスタンス変数（初期値はnull）
    private static TaskTrayService _instance;

    // 2. スレッドセーフのためのロックオブジェクト
    private static readonly object _lock = new object();

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

    // keep references to menu items so we can update text on culture change
    private ToolStripItem _openSettingsMenuItem;
    private ToolStripItem _exitMenuItem;

    // handler reference so we can unsubscribe
    private PropertyChangedEventHandler _localizationHandler;

    // イベントを定義
    public event EventHandler ClipboardOperationRequested;
    public event EventHandler ShowWindowRequested;
    public event EventHandler ExitApplicationRequested;

    public void Initialize()
    {
        // 既に初期化済みの場合は何もしない
        if (_isInitialized)
        {
            Debug.WriteLine("TaskTrayService is already initialized.");
            return;
        }

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

        var menu = new ContextMenuStrip();

        // Use localized strings
        _openSettingsMenuItem = menu.Items.Add(LocalizedStrings.Instance.OpenSettingText, null, OnShowClicked);
        _exitMenuItem = menu.Items.Add(LocalizedStrings.Instance.ExitText, null, OnExitClicked);

        // subscribe to localization changes to update menu text dynamically
        _localizationHandler = (s, e) =>
        {
            try
            {
                if (e.PropertyName == nameof(LocalizedStrings.OpenSettingText) && _openSettingsMenuItem != null)
                {
                    _openSettingsMenuItem.Text = LocalizedStrings.Instance.OpenSettingText;
                }
                else if (e.PropertyName == nameof(LocalizedStrings.ExitText) && _exitMenuItem != null)
                {
                    _exitMenuItem.Text = LocalizedStrings.Instance.ExitText;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TaskTrayService: localization update failed: {ex}");
            }
        };

        LocalizedStrings.Instance.PropertyChanged += _localizationHandler;

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Icon = new Icon(iconStream),
            Text = "Clipboard Utility",
            ContextMenuStrip = menu
        };

        _notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(OnNotifyIconClicked);
        _isInitialized = true;

        Debug.WriteLine("TaskTrayService initialized successfully.");
    }

    private void OnNotifyIconClicked(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // イベントを発火
            ClipboardOperationRequested?.Invoke(this, EventArgs.Empty);
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
        if (_notifyIcon != null)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        _isInitialized = false;

        // unsubscribe localization handler
        try
        {
            if (_localizationHandler != null)
            {
                LocalizedStrings.Instance.PropertyChanged -= _localizationHandler;
                _localizationHandler = null;
            }
        }
        catch { }

        // Singletonインスタンスもリセット
        lock (_lock)
        {
            _instance = null;
        }
    }
}
