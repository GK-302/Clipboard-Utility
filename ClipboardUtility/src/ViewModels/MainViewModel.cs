using ClipboardUtility.Services;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using ClipboardUtility.src.Properties;
using System.Windows.Media;
using System.Diagnostics;

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

    public void DoClipboardOperation()
    {
        if (System.Windows.Clipboard.ContainsText())
        {
            _isInternalClipboardOperation = true;

            try
            {
                string clipboardText = System.Windows.Clipboard.GetText();

                // Use processing mode from settings
                string processedText = _textProcessingService.Process(clipboardText, _appSettings.ClipboardProcessingMode);

                // フラグをセットしたままクリップボードを更新
                System.Windows.Clipboard.SetText(processedText);
                // 操作完了の通知のみ表示
                string notificationMessage = _appSettings.ClipboardProcessingMode.GetNotificationMessage();
                _ = _notificationsService.ShowNotification(notificationMessage, NotificationType.Operation);
                Debug.WriteLine("Clipboard operation completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during clipboard operation: {ex.Message}");
            }
            finally
            {
                // 少し遅延してからフラグをリセット（クリップボードイベントの伝播を待つ）
                Task.Delay(100).ContinueWith(_ => _isInternalClipboardOperation = false);
            }
        }
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
