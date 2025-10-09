using ClipboardUtility.Services;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Properties;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ClipboardUtility.src.ViewModels;

/// <summary>
/// メインウィンドウのViewModel。UIのロジックと状態を管理します。
/// INotifyPropertyChangedを実装し、プロパティの変更をUIに通知します。
/// 設定の管理はSettingsServiceに委譲し、必要時に参照する責任分離設計を採用。
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ClipboardService _clipboardService;
    private readonly TextProcessingService _textProcessingService;
    private readonly NotificationsService _notificationsService;
    private bool _isInternalClipboardOperation = false;

    public event PropertyChangedEventHandler PropertyChanged;

    public MainViewModel()
    {
        _clipboardService = new ClipboardService();
        _textProcessingService = new TextProcessingService();
        _notificationsService = new NotificationsService();
        
        // 設定変更通知を購読（設定の直接保持はやめて参照のみ）
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
        
        Debug.WriteLine($"MainViewModel: Initialized with ClipboardProcessingMode = {SettingsService.Instance.Current.ClipboardProcessingMode}");
    }

    /// <summary>
    /// 設定変更時のイベントハンドラー
    /// 設定は直接保持せず、SettingsServiceから必要時に取得する方式に変更
    /// </summary>
    private void OnSettingsChanged(object sender, AppSettings newSettings)
    {
        try
        {
            Debug.WriteLine($"MainViewModel: SettingsChanged event received. New ClipboardProcessingMode = {newSettings.ClipboardProcessingMode}");
            Debug.WriteLine($"MainViewModel: Settings will be retrieved from SettingsService when needed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainViewModel: Error handling settings change: {ex.Message}");
        }
    }

    /// <summary>
    /// 現在の設定を取得するヘルパーメソッド
    /// </summary>
    private AppSettings GetCurrentSettings() => SettingsService.Instance.Current;

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
        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
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
        Debug.WriteLine("Clipboard updated");
        if (_isInternalClipboardOperation)
        {
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
            Debug.WriteLine("MainViewModel: No text content in clipboard");
            return;
        }

        _isInternalClipboardOperation = true;

        try
        {
            var currentSettings = GetCurrentSettings();
            
            // 現在の設定を使用して処理
            Debug.WriteLine($"MainViewModel: Processing clipboard with mode {currentSettings.ClipboardProcessingMode}");
            string processedText = _textProcessingService.Process(clipboardText, currentSettings.ClipboardProcessingMode);

            // 非同期でクリップボードを更新し、検証も行う
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _clipboardService.SetTextAsync(processedText, cts.Token);

            if (success)
            {
                await ShowOperationSuccessNotification(currentSettings.ClipboardProcessingMode);
            }
            else
            {
                Debug.WriteLine("MainViewModel: Primary operation failed, attempting fallback");
                await AttemptFallbackOperation(processedText, currentSettings.ClipboardProcessingMode, cts.Token);
            }
        }
        catch (TaskCanceledException tce)
        {
            Debug.WriteLine($"MainViewModel: Clipboard operation cancelled: {tce.Message}");
            Trace.WriteLine(tce.ToString());
            _ = _notificationsService.ShowNotification("クリップボード操作がタイムアウトしました", NotificationType.Operation);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"MainViewModel: Error during clipboard operation: {ex.Message}");
        }
        finally
        {
            await Task.Delay(200);
            _isInternalClipboardOperation = false;
            Debug.WriteLine("MainViewModel: Operation completed, internal flag reset");
        }
    }

    /// <summary>
    /// 操作成功時の通知を表示
    /// </summary>
    private async Task ShowOperationSuccessNotification(ProcessingMode mode)
    {
        string notificationMessage = mode.GetNotificationMessage();
        await _notificationsService.ShowNotification(notificationMessage, NotificationType.Operation);
        Debug.WriteLine($"MainViewModel: Operation completed successfully with message: {notificationMessage}");
    }

    /// <summary>
    /// フォールバック操作を試行
    /// </summary>
    private async Task AttemptFallbackOperation(string processedText, ProcessingMode mode, CancellationToken cancellationToken)
    {
        try
        {
            _clipboardService.SetText(processedText);
            await Task.Delay(100, cancellationToken);
            
            bool verified = await _clipboardService.VerifyClipboardContentAsync(processedText, cancellationToken);
            if (verified)
            {
                await ShowOperationSuccessNotification(mode);
                Debug.WriteLine("MainViewModel: Fallback operation succeeded");
            }
            else
            {
                Debug.WriteLine("MainViewModel: Fallback operation failed - verification failed");
            }
        }
        catch (Exception fallbackEx)
        {
            Debug.WriteLine($"MainViewModel: Fallback operation failed: {fallbackEx.Message}");
        }
    }

    public void DoClipboardOperation()
    {
        // 非同期版を呼び出す（fire-and-forget）
        _ = DoClipboardOperationAsync();
    }

    /// <summary>
    /// 設定ウィンドウを開く
    /// SettingsServiceから現在の設定を取得して渡す
    /// </summary>
    public void OpenSettings()
    {
        try
        {
            var currentSettings = GetCurrentSettings();
            var settingsWindow = new SettingsWindow(currentSettings);
            
            bool? result = settingsWindow.ShowDialog();
            
            if (result == true)
            {
                Debug.WriteLine("MainViewModel: Settings saved by user");
            }
            else
            {
                Debug.WriteLine("MainViewModel: Settings dialog cancelled by user");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainViewModel: Failed to open settings window: {ex.Message}");
        }
    }

    /// <summary>
    /// リソースのクリーンアップ
    /// すべてのサービスの適切な解放を行う
    /// </summary>
    public void Cleanup()
    {
        try
        {
            // イベント購読を解除
            SettingsService.Instance.SettingsChanged -= OnSettingsChanged;
            Debug.WriteLine("MainViewModel: Settings event subscription removed");
            
            // クリップボードサービスの解放
            _clipboardService?.Dispose();
            Debug.WriteLine("MainViewModel: ClipboardService disposed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainViewModel: Error during cleanup: {ex.Message}");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
