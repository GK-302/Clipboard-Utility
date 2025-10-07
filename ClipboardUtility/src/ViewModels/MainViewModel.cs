using ClipboardUtility.Services;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Properties;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClipboardUtility.src.ViewModels;

/// <summary>
/// メインウィンドウのViewModel。UIのロジックと状態を管理します。
/// INotifyPropertyChangedを実装し、プロパティの変更をUIに通知します。
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ClipboardService _clipboardService;
    private string _ClipboardLabel = string.Empty;
    private TextProcessingService _textProcessingService = new();
    private NotificationsService _notificationsService = new();
    private bool _isInternalClipboardOperation = false;

    // App settings (loaded from appsettings.json or defaults)
    private readonly AppSettings _appSettings;

    public event PropertyChangedEventHandler PropertyChanged;

    public MainViewModel()
    {
        _clipboardService = new ClipboardService();
        _appSettings = AppSettings.Load();
    }

    /// <summary>
    /// TaskTrayServiceのイベントを購読する
    /// </summary>
    /// <param name="taskTrayService">TaskTrayServiceのインスタンス</param>
    public void SubscribeToTaskTrayEvents(TaskTrayService taskTrayService)
    {
        taskTrayService.ClipboardOperationRequested += OnClipboardOperationRequested;
        taskTrayService.ShowWindowRequested += OnShowWindowRequested;
        taskTrayService.ExitApplicationRequested += OnExitApplicationRequested;
    }

    private void OnClipboardOperationRequested(object sender, EventArgs e)
    {
        DoClipboardOperation();
    }

    private void OnShowWindowRequested(object sender, EventArgs e)
    {
        // NotifyIcon のイベントは UI スレッドではない可能性があるので Dispatcher 経由で開く
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                OpenSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenSettings failed: {ex.Message}");
            }
        }));
    }

    private void OnExitApplicationRequested(object sender, EventArgs e)
    {
        // アプリケーション終了ロジック
        System.Windows.Application.Current.Shutdown();
    }

    public void Initialize(System.Windows.Window window)
    {
        _clipboardService.StartMonitoring(window);
        _clipboardService.ClipboardUpdated += OnClipboardUpdated;
    }

    private void OnClipboardUpdated(object sender, string newText)
    {
        if (_isInternalClipboardOperation)
        {
            return;
        }

        // クリップボードへのアクセス確認
        if (!TryAccessClipboard(out string clipboardText))
        {
            // アクセス拒否時の通知
            _ = _notificationsService.ShowNotification("クリップボードへのアクセスが拒否されました", NotificationType.Copy);
            return;
        }

        int charCount = _textProcessingService.CountCharacters(newText);
        string formattedText;
        try
        {
            formattedText = string.Format(Resources.NotificationFormat_CopiedWords, charCount);
        }
        catch (FormatException ex)
        {
            Debug.WriteLine($"Format error: {ex.Message}");
            formattedText = $"Copied {charCount} words";
        }

        // 内部処理中でない場合のみ通知
        _ = _notificationsService.ShowNotification(formattedText, NotificationType.Copy);
    }

    /// <summary>
    /// クリップボードアクセスを安全に試行する
    /// </summary>
    /// <param name="text">取得されたテキスト</param>
    /// <returns>アクセス成功時true</returns>
    private bool TryAccessClipboard(out string text)
    {
        text = string.Empty;
        const int maxRetries = 3;
        const int delayMs = 50;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    text = System.Windows.Clipboard.GetText();
                    return true;
                }
                return false;
            }
            catch (COMException ex) when (ex.HResult == -2147221040) // CLIPBRD_E_CANT_OPEN
            {
                Debug.WriteLine($"Clipboard access denied (attempt {i + 1}): {ex.Message}");
                if (i < maxRetries - 1)
                {
                    Task.Delay(delayMs).Wait();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected clipboard error: {ex.Message}");
                return false;
            }
        }
        return false;
    }

    public void DoClipboardOperation()
    {
        // クリップボードアクセスを安全に試行
        if (!TryAccessClipboard(out string clipboardText))
        {
            // アクセス拒否時の通知
            _ = _notificationsService.ShowNotification("クリップボードへのアクセスが拒否されました", NotificationType.Operation);
            return;
        }

        _isInternalClipboardOperation = true;

        try
        {
            // Use processing mode from settings
            string processedText = _textProcessingService.Process(clipboardText, _appSettings.ClipboardProcessingMode);

            // リトライ機構付きでクリップボードを設定
            if (TrySetClipboardText(processedText))
            {
                string notificationMessage = _appSettings.ClipboardProcessingMode.GetNotificationMessage();
                _ = _notificationsService.ShowNotification(notificationMessage, NotificationType.Operation);
                Debug.WriteLine("Clipboard operation completed");
            }
            else
            {
                // 設定失敗時の通知
                _ = _notificationsService.ShowNotification("クリップボードの更新に失敗しました", NotificationType.Operation);
                Debug.WriteLine("Failed to set clipboard text after retries");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during clipboard operation: {ex.Message}");
            _ = _notificationsService.ShowNotification("クリップボード操作中にエラーが発生しました", NotificationType.Operation);
            // 例: Resources.NotificationFormat_ClipboardAccessDenied
            //_ = _notificationsService.ShowNotification(
            //    Resources.NotificationFormat_ClipboardAccessDenied ?? "クリップボードへのアクセスが拒否されました",
            //    NotificationType.Operation
            //);

        }
        finally
        {
            // 少し遅延してからフラグをリセット（クリップボードイベントの伝播を待つ）
            Task.Delay(100).ContinueWith(_ => _isInternalClipboardOperation = false);
        }
    }

    /// <summary>
    /// リトライ機構付きクリップボードテキスト設定
    /// </summary>
    private bool TrySetClipboardText(string text)
    {
        const int maxRetries = 3;
        const int delayMs = 50;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return true;
            }
            catch (COMException ex) when (ex.HResult == -2147221040) // CLIPBRD_E_CANT_OPEN
            {
                Debug.WriteLine($"Clipboard set failed (attempt {i + 1}): {ex.Message}");
                if (i < maxRetries - 1)
                {
                    Task.Delay(delayMs).Wait();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error setting clipboard: {ex.Message}");
                return false;
            }
        }
        return false;
    }

    // Add method to open settings UI
    public void OpenSettings()
    {
        // 明示的にランタイムの現在設定を渡す（依存を明示）
        var window = new SettingsWindow(SettingsService.Instance.Current);
        bool? result = null;
        try
        {
            result = window.ShowDialog();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OpenSettings: failed to show settings window: {ex.Message}");
        }

        if (result == true)
        {
            Debug.WriteLine("Settings saved by user.");
        }
    }

    public void Cleanup()
    {
        _clipboardService?.Dispose();
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
