using ClipboardUtility.Services;
using ClipboardUtility.src.Services;
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

    public event PropertyChangedEventHandler PropertyChanged;

    public MainViewModel()
    {
        _clipboardService = new ClipboardService();
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
        // ウィンドウ表示ロジック
        Debug.WriteLine("Show window requested");
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
        _ = _notificationsService.ShowNotification(formattedText);
    }

    public void DoClipboardOperation()
    {
        if (System.Windows.Clipboard.ContainsText())
        {
            _isInternalClipboardOperation = true;

            try
            {
                string clipboardText = System.Windows.Clipboard.GetText();
                string processedText = clipboardText.Replace("\r\n", " ").Replace("\n", " ");

                // フラグをセットしたままクリップボードを更新
                System.Windows.Clipboard.SetText(processedText);

                // 操作完了の通知のみ表示
                _ = _notificationsService.ShowNotification("removed");
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

    public void Cleanup()
    {
        _clipboardService?.Dispose();
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
