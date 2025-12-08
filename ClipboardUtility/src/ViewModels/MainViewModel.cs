using ClipboardUtility.Services;
using ClipboardUtility.src.Coordinators;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Views;
using System.ComponentModel;
using System.Diagnostics;

namespace ClipboardUtility.src.ViewModels;

/// <summary>
/// メインウィンドウのViewModel。UI関連のロジック（ダイアログ表示、タスクトレイ連携、初期化・破棄）を管理します。
/// クリップボード操作は ClipboardOperationService、イベント処理は ClipboardEventCoordinator に委譲する責任分離設計を採用。
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ClipboardService _clipboardService;
    private readonly ClipboardOperationService _clipboardOperationService;
    private readonly ClipboardEventCoordinator _clipboardEventCoordinator;
    private readonly SettingsService _settingsService;
    private readonly ICultureProvider _cultureProvider;
    private readonly IAppRestartService _restartService;
    private bool _isInternalClipboardOperation = false;
    private readonly TextProcessingService _textProcessingService;
    private readonly NotificationsService _notificationsService;
    private readonly TaskTrayService _taskTrayService;
    private readonly PresetService _presetService;
    private readonly UpdateCheckService _updateCheckService;

    // デバッグ用: _isInternalClipboardOperation の変更を追跡
    private void SetInternalClipboardOperation(bool value)
    {
        if (_isInternalClipboardOperation != value)
        {
            Debug.WriteLine($"MainViewModel: _isInternalClipboardOperation changed from {_isInternalClipboardOperation} to {value} at {DateTime.Now:HH:mm:ss.fff}");
            _isInternalClipboardOperation = value;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public MainViewModel(
            ClipboardService clipboardService,
            ClipboardOperationService clipboardOperationService,
            SettingsService settingsService,
            TaskTrayService taskTrayService,
            IAppRestartService restartService,
            ICultureProvider cultureProvider,
            TextProcessingService textProcessingService,
            NotificationsService notificationsService,
            PresetService presetService,
            UpdateCheckService updateCheckService)
    {
        _clipboardService = clipboardService;
        _clipboardOperationService = clipboardOperationService;
        _settingsService = settingsService;
        _restartService = restartService;
        _cultureProvider = cultureProvider;
        _taskTrayService = taskTrayService;
        _textProcessingService = textProcessingService;
        _notificationsService = notificationsService;
        _presetService = presetService;
        _updateCheckService = updateCheckService;
        //　ClipboardEventCoordinator を手動で 'new' する
        _clipboardEventCoordinator = new ClipboardEventCoordinator(
            _textProcessingService,
            _notificationsService,
            () => _isInternalClipboardOperation, // Func<bool> を渡す
            _settingsService,
            _taskTrayService,
            _clipboardService
        );
        // 注入された SettingsService を使用
        _settingsService.SettingsChanged += OnSettingsChanged;

        Debug.WriteLine($"MainViewModel: Initialized with ClipboardProcessingMode = {_settingsService.Current.ClipboardProcessingMode}");

        // イベント購読（コーディネータに委譲）
        _clipboardService.ClipboardUpdated += _clipboardEventCoordinator.OnClipboardUpdated;
        _clipboardService.ClipboardError += _clipboardEventCoordinator.OnClipboardError;

        // <--- 追加: 注入された TaskTrayService を使用 (App.xaml.cs からロジックを移動)
        SubscribeToTaskTrayEvents(taskTrayService);
    }

    /// <summary>
    /// 設定変更時のイベントハンドラー
    /// </summary>
    private void OnSettingsChanged(object sender, AppSettings newSettings)
    {
        try
        {
            Debug.WriteLine($"MainViewModel: SettingsChanged event received. New ClipboardProcessingMode = {newSettings.ClipboardProcessingMode}");
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
    }

    public async Task DoClipboardOperationAsync()
    {
        Debug.WriteLine($"MainViewModel: DoClipboardOperationAsync called at {DateTime.Now:HH:mm:ss.fff}");
        _ = await _clipboardOperationService.ExecuteClipboardOperationAsync(
            () => _isInternalClipboardOperation,
            SetInternalClipboardOperation);
        Debug.WriteLine($"MainViewModel: DoClipboardOperationAsync completed at {DateTime.Now:HH:mm:ss.fff}");
    }

    public void DoClipboardOperation()
    {
        // 非同期版を呼び出す（fire-and-forget）
        _ = DoClipboardOperationAsync();
    }

    /// <summary>
    /// 設定ウィンドウを開く
    /// </summary>
    public void OpenSettings()
    {
        try
        {
            var currentSettings = _settingsService.Current;
            var settingsWindow = new SettingsWindow(
                                        currentSettings,
                                        _restartService,
                                        _cultureProvider,
                                        _settingsService,
                                        _presetService,
                                        _textProcessingService,
                                        _updateCheckService);
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
    /// </summary>
    public void Cleanup()
    {
        try
        {
            // イベント購読を解除
            _settingsService.SettingsChanged -= OnSettingsChanged;
            _clipboardService.ClipboardUpdated -= _clipboardEventCoordinator.OnClipboardUpdated;
            _clipboardService.ClipboardError -= _clipboardEventCoordinator.OnClipboardError;
            Debug.WriteLine("MainViewModel: Event subscriptions removed");

            // クリップボードサービスの解放
            _clipboardService?.Dispose();
            Debug.WriteLine("MainViewModel: ClipboardService disposed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainViewModel: Error during cleanup: {ex.Message}");
        }
    }
}
