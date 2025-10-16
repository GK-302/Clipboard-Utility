using ClipboardUtility.Services;
using System.Diagnostics;

namespace ClipboardUtility.src.Services;

/// <summary>
/// �N���b�v�{�[�h����̎��s��Ӗ��Ƃ���T�[�r�X�B
/// �ǂݎ�� �� �e�L�X�g���� �� �����߂� �� ���� �� �t�H�[���o�b�N�̈�A�̃t���[���Ǘ����܂��B
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
    /// �N���b�v�{�[�h�����񓯊��Ŏ��s���܂��B
    /// </summary>
    /// <param name="isInternalOperation">��������t���O�i�Ăяo�����ŊǗ��j</param>
    /// <returns>���삪�����������ǂ���</returns>
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

            // UsePresets���L���ȏꍇ�̓v���Z�b�g�����s�A�����łȂ���Βʏ�̏������[�h�����s
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
                // UsePresets���L���ȏꍇ�͐�p�̒ʒm��\��
                if (currentSettings.UsePresets && currentSettings.SelectedPresetId.HasValue)
                {
                    var preset = _presetService.GetPresetById(currentSettings.SelectedPresetId.Value);
                    if (preset != null)
                    {
                        await _notificationsService.ShowNotification($"�v���Z�b�g���s: {preset.Name}", NotificationType.Operation);
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
            await _notificationsService.ShowNotification("�N���b�v�{�[�h���삪�^�C���A�E�g���܂���", NotificationType.Operation);
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
    /// ���쐬�����̒ʒm��\��
    /// </summary>
    private async Task ShowOperationSuccessNotificationAsync(ProcessingMode mode)
    {
        string notificationMessage = mode.GetNotificationMessage();
        await _notificationsService.ShowNotification(notificationMessage, NotificationType.Operation);
        Debug.WriteLine($"ClipboardOperationService: Operation completed successfully with message: {notificationMessage}");
    }

    /// <summary>
    /// �t�H�[���o�b�N��������s
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
