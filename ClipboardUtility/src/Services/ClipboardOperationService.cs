using ClipboardUtility.Services;
using System.Diagnostics;

namespace ClipboardUtility.src.Services;

/// <summary>
/// クリップボード操作の実行を責務とするサービス。
/// 読み取り → テキスト処理 → 書き戻し → 検証 → フォールバックの一連のフローを管理します。
/// </summary>
internal class ClipboardOperationService
{
    private readonly ClipboardService _clipboardService;
    private readonly TextProcessingService _textProcessingService;
    private readonly NotificationsService _notificationsService;
    private readonly PresetService _presetService;

    public ClipboardOperationService(
        ClipboardService clipboardService,
        TextProcessingService textProcessingService,
        NotificationsService notificationsService,
        PresetService presetService)
    {
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _textProcessingService = textProcessingService ?? throw new ArgumentNullException(nameof(textProcessingService));
        _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
        _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
    }

    /// <summary>
    /// クリップボード操作を非同期で実行します。
    /// </summary>
    /// <param name="isInternalOperation">内部操作フラグ（呼び出し元で管理）</param>
    /// <returns>操作が完了したかどうか</returns>
    public async Task<bool> ExecuteClipboardOperationAsync(Func<bool> isInternalOperation, Action<bool> setInternalOperation)
    {
        string clipboardText = _clipboardService.GetTextSafely();
        if (string.IsNullOrEmpty(clipboardText))
        {
            Debug.WriteLine("ClipboardOperationService: No text content in clipboard");
            return false;
        }

        setInternalOperation(true);

        try
        {
            var currentSettings = SettingsService.Instance.Current;

            string processedText;

            // UsePresetsが有効な場合はプリセットを実行、そうでなければ通常の処理モードを実行
            if (currentSettings.UsePresets && currentSettings.SelectedPresetId.HasValue)
            {
                Debug.WriteLine($"ClipboardOperationService: Using preset mode. Preset ID: {currentSettings.SelectedPresetId.Value}");
                var preset = _presetService.GetPresetById(currentSettings.SelectedPresetId.Value);
                
                if (preset != null)
                {
                    Debug.WriteLine($"ClipboardOperationService: Executing preset '{preset.Name}' (ID: {preset.Id})");
                    processedText = _presetService.ExecutePreset(preset, clipboardText);
                }
                else
                {
                    Debug.WriteLine($"ClipboardOperationService: Preset not found (ID: {currentSettings.SelectedPresetId.Value}), falling back to normal mode");
                    processedText = _textProcessingService.Process(clipboardText, currentSettings.ClipboardProcessingMode);
                }
            }
            else
            {
                Debug.WriteLine($"ClipboardOperationService: Processing clipboard with mode {currentSettings.ClipboardProcessingMode}");
                processedText = _textProcessingService.Process(clipboardText, currentSettings.ClipboardProcessingMode);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _clipboardService.SetTextAsync(processedText, cts.Token);

            if (success)
            {
                // UsePresetsが有効な場合は専用の通知を表示
                if (currentSettings.UsePresets && currentSettings.SelectedPresetId.HasValue)
                {
                    var preset = _presetService.GetPresetById(currentSettings.SelectedPresetId.Value);
                    if (preset != null)
                    {
                        await _notificationsService.ShowNotification($"プリセット実行: {preset.Name}", NotificationType.Operation);
                        Debug.WriteLine($"ClipboardOperationService: Preset '{preset.Name}' executed successfully");
                    }
                    else
                    {
                        await ShowOperationSuccessNotificationAsync(currentSettings.ClipboardProcessingMode);
                    }
                }
                else
                {
                    await ShowOperationSuccessNotificationAsync(currentSettings.ClipboardProcessingMode);
                }
                return true;
            }
            else
            {
                Debug.WriteLine("ClipboardOperationService: Primary operation failed, attempting fallback");
                return await AttemptFallbackOperationAsync(processedText, currentSettings.ClipboardProcessingMode, cts.Token);
            }
        }
        catch (TaskCanceledException tce)
        {
            Debug.WriteLine($"ClipboardOperationService: Operation cancelled: {tce.Message}");
            Debug.WriteLine(tce.ToString());
            await _notificationsService.ShowNotification("クリップボード操作がタイムアウトしました", NotificationType.Operation);
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardOperationService: Error during operation: {ex.Message}");
            return false;
        }
        finally
        {
            await Task.Delay(200);
            setInternalOperation(false);
            Debug.WriteLine("ClipboardOperationService: Operation completed, internal flag reset");
        }
    }

    /// <summary>
    /// 操作成功時の通知を表示
    /// </summary>
    private async Task ShowOperationSuccessNotificationAsync(ProcessingMode mode)
    {
        string notificationMessage = mode.GetNotificationMessage();
        await _notificationsService.ShowNotification(notificationMessage, NotificationType.Operation);
        Debug.WriteLine($"ClipboardOperationService: Operation completed successfully with message: {notificationMessage}");
    }

    /// <summary>
    /// フォールバック操作を試行
    /// </summary>
    private async Task<bool> AttemptFallbackOperationAsync(string processedText, ProcessingMode mode, CancellationToken cancellationToken)
    {
        try
        {
            _clipboardService.SetText(processedText);
            await Task.Delay(100, cancellationToken);

            bool verified = await _clipboardService.VerifyClipboardContentAsync(processedText, cancellationToken);
            if (verified)
            {
                await ShowOperationSuccessNotificationAsync(mode);
                Debug.WriteLine("ClipboardOperationService: Fallback operation succeeded");
                return true;
            }
            else
            {
                Debug.WriteLine("ClipboardOperationService: Fallback operation failed - verification failed");
                return false;
            }
        }
        catch (Exception fallbackEx)
        {
            Debug.WriteLine($"ClipboardOperationService: Fallback operation failed: {fallbackEx.Message}");
            return false;
        }
    }
}
