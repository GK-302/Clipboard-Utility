using ClipboardUtility.Services;
using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Properties;
using ClipboardUtility.src.Services;
using System.Diagnostics;
using static ClipboardUtility.src.Services.ClipboardService;

namespace ClipboardUtility.src.Coordinators;

/// <summary>
/// クリップボードイベントを受け取り、適切な処理（通知表示・エラーハンドリング）に振り分ける調整役。
/// </summary>
internal class ClipboardEventCoordinator
{
    private readonly TextProcessingService _textProcessingService;
    private readonly NotificationsService _notificationsService;
    private readonly Func<bool> _isInternalOperationGetter;

    public ClipboardEventCoordinator(
        TextProcessingService textProcessingService,
        NotificationsService notificationsService,
        Func<bool> isInternalOperationGetter)
    {
        _textProcessingService = textProcessingService ?? throw new ArgumentNullException(nameof(textProcessingService));
        _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
        _isInternalOperationGetter = isInternalOperationGetter ?? throw new ArgumentNullException(nameof(isInternalOperationGetter));
    }

    /// <summary>
    /// ClipboardService.ClipboardUpdated イベントハンドラ
    /// </summary>
    public void OnClipboardUpdated(object sender, string newText)
    {
        Debug.WriteLine("ClipboardEventCoordinator: Clipboard updated");

        if (_isInternalOperationGetter())
        {
            Debug.WriteLine("ClipboardEventCoordinator: Skipping update (internal operation)");
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
            Debug.WriteLine($"ClipboardEventCoordinator: Format error: {ex.Message}");
            formattedText = $"Copied {charCount} words";
        }

        _ = _notificationsService.ShowNotification(formattedText, NotificationType.Copy);
    }

    /// <summary>
    /// ClipboardService.ClipboardError イベントハンドラ
    /// </summary>
    public void OnClipboardError(object? sender, ClipboardErrorEventArgs e)
    {
        Debug.WriteLine($"ClipboardEventCoordinator: Clipboard error ({e.Context}): {e.Exception.Message}");

        // UI 更新は Dispatcher 経由で実行
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                // 設定で通知が抑止されていないか確認
                if (SettingsService.Instance.Current.ShowOperationNotification)
                {
                    _ = _notificationsService.ShowNotification(
                        LocalizedStrings.Instance.ClipboardAccessDeniedMessage,
                        NotificationType.Operation);
                }
                else
                {
                    Debug.WriteLine("ClipboardEventCoordinator: ShowOperationNotification is disabled; skipping user notification.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClipboardEventCoordinator: Failed to show clipboard error notification: {ex.Message}");
                FileLogger.LogException(ex, "ClipboardEventCoordinator.OnClipboardError");
            }
        });
    }
}
