using ClipboardUtility.Services;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Properties;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

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

    // App settings (SettingsServiceから動的に取得)
    private AppSettings _appSettings;

    public event PropertyChangedEventHandler PropertyChanged;

    public MainViewModel()
    {
        _clipboardService = new ClipboardService();
        
        // SettingsServiceから設定を取得
        _appSettings = SettingsService.Instance.Current;
        
        // 設定変更通知を購読
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
        
        Debug.WriteLine($"MainViewModel: Initial ClipboardProcessingMode = {_appSettings.ClipboardProcessingMode}");
    }

    /// <summary>
    /// 設定変更時のイベントハンドラー
    /// </summary>
    private void OnSettingsChanged(object sender, AppSettings newSettings)
    {
        try
        {
            Debug.WriteLine($"MainViewModel: SettingsChanged event received. New ClipboardProcessingMode = {newSettings.ClipboardProcessingMode}");
            
            // UIスレッドで設定を更新
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _appSettings = newSettings;
                Debug.WriteLine($"MainViewModel: Settings updated. Current ClipboardProcessingMode = {_appSettings.ClipboardProcessingMode}");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainViewModel: Error handling settings change: {ex.Message}");
        }
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

    public async Task DoClipboardOperationAsync()
    {
        string clipboardText = _clipboardService.GetTextSafely();
        if (string.IsNullOrEmpty(clipboardText))
        {
            Debug.WriteLine("No text content in clipboard");
            return;
        }

        _isInternalClipboardOperation = true;

        try
        {
            // 現在の設定を使用して処理
            Debug.WriteLine($"MainViewModel: Processing clipboard with mode {_appSettings.ClipboardProcessingMode}");
            string processedText = _textProcessingService.Process(clipboardText, _appSettings.ClipboardProcessingMode);

            // 非同期でクリップボードを更新し、検証も行う
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5秒でタイムアウト
            bool success = await _clipboardService.SetTextAsync(processedText, cts.Token);

            if (success)
            {
                // 操作成功の通知を表示（現在の設定から通知メッセージを取得）
                string notificationMessage = _appSettings.ClipboardProcessingMode.GetNotificationMessage();
                _ = _notificationsService.ShowNotification(notificationMessage, NotificationType.Operation);
                Debug.WriteLine($"MainViewModel: Clipboard operation completed successfully with message: {notificationMessage}");
            }
            else
            {
                Debug.WriteLine("Clipboard operation failed - content verification failed");
                // フォールバック: 従来の方法で再試行
                try
                {
                    _clipboardService.SetText(processedText);
                    // 短い遅延後に検証
                    await Task.Delay(100, cts.Token);
                    bool verified = await _clipboardService.VerifyClipboardContentAsync(processedText, cts.Token);
                    if (verified)
                    {
                        string notificationMessage = _appSettings.ClipboardProcessingMode.GetNotificationMessage();
                        _ = _notificationsService.ShowNotification(notificationMessage, NotificationType.Operation);
                        Debug.WriteLine("Clipboard operation completed with fallback method");
                    }
                    else
                    {
                        Debug.WriteLine("Clipboard operation failed even with fallback method");
                    }
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"Fallback clipboard operation failed: {fallbackEx.Message}");
                }
            }
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("Clipboard operation was cancelled due to timeout");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during clipboard operation: {ex.Message}");
        }
        finally
        {
            // 少し遅延してからフラグをリセット（クリップボードイベントの伝播を待つ）
            await Task.Delay(200);
            _isInternalClipboardOperation = false;
        }
    }

    public void DoClipboardOperation()
    {
        // 非同期版を呼び出す（fire-and-forget）
        _ = DoClipboardOperationAsync();
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
        // イベント購読を解除
        SettingsService.Instance.SettingsChanged -= OnSettingsChanged;
        _clipboardService?.Dispose();
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
