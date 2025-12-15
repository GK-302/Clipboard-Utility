using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Common;
using System; // EnumやException用に必要
using System.Collections.Generic; // List用に必要
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Task用に必要
using System.Windows.Input;

namespace ClipboardUtility.src.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    private AppSettings _settings;
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly SettingsService _settingsService;
    private readonly PresetService _presetService;
    private readonly UpdateCheckService _updateCheckService;
    private readonly TextProcessingService _textProcessingService; // コンストラクタ引数にあるためフィールド化

    private bool _isRefreshingPresets;

    // ■ MSIX環境かどうかの判定用プロパティ（Viewへのバインド用）
    public bool IsMsix => UpdateCheckService.IsMsix;

    // 更新チェック中フラグ
    private bool _isCheckingForUpdates;
    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        set
        {
            if (_isCheckingForUpdates != value)
            {
                _isCheckingForUpdates = value;
                OnPropertyChanged();
                // IsCheckingForUpdatesが変わるとCanCheckForUpdatesの結果も変わるため通知
                OnPropertyChanged(nameof(CanCheckForUpdates));
            }
        }
    }

    // ■ MSIXの場合は常に false (更新チェック不可) を返す
    public bool CanCheckForUpdates => !IsMsix && !IsCheckingForUpdates;

    // 現在のバージョン
    public string CurrentVersion => UpdateCheckService.GetCurrentVersion().ToString();

    // コマンド
    public ICommand CheckForUpdatesCommand { get; }

    public SettingsViewModel(
            AppSettings settings,
            ICultureProvider cultureProvider,
            SettingsService settingsService,
            PresetService presetService,
            TextProcessingService textProcessingService,
            UpdateCheckService updateCheckService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _updateCheckService = updateCheckService ?? throw new ArgumentNullException(nameof(updateCheckService));
        _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
        _textProcessingService = textProcessingService; // フィールドに保持

        _settings = _settingsService.Current;

        _settingsService.SettingsChanged += OnSettingsServiceChanged;
        Debug.WriteLine($"SettingsViewModel: Subscribed to SettingsService.SettingsChanged. Initial Current hash={_settings?.GetHashCode()} SelectedPresetId={_settings?.SelectedPresetId}");

        _processingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();

        // 利用可能なカルチャ一覧
        var available = cultureProvider?.AvailableCultures ?? new List<CultureInfo> { CultureInfo.CurrentUICulture };
        AvailableCultures = available.ToList();

        _presetService.LoadPresets();
        AvailablePresets = new ObservableCollection<ProcessingPreset>(_presetService.Presets);

        SelectedPreset = AvailablePresets.FirstOrDefault();
        _usePresets = _settings.UsePresets;

        // タスクトレイクリック用のプリセット復元ロジック
        if (_settings.SelectedPresetId.HasValue)
        {
            Debug.WriteLine($"SettingsViewModel: Loading preset from settings, ID = {_settings.SelectedPresetId.Value}");
            SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault(p => p.Id == _settings.SelectedPresetId.Value);

            if (SelectedPresetForTrayClick == null)
            {
                Debug.WriteLine("SettingsViewModel: Preset not found, using first preset as fallback");
                SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault();
            }
        }
        else
        {
            Debug.WriteLine("SettingsViewModel: No preset ID in settings, using first preset");
            SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault();
            _settings.SelectedPresetId = SelectedPresetForTrayClick?.Id;
        }

        // カルチャの初期選択
        SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Name == (_settings.CultureName ?? CultureInfo.CurrentUICulture.Name))
                          ?? CultureInfo.CurrentUICulture;

        // ■ コマンドの初期化（CanExecute条件に CanCheckForUpdates プロパティを使用）
        CheckForUpdatesCommand = new RelayCommand(async _ => await CheckForUpdatesAsync(), _ => CanCheckForUpdates);
    }

    private void OnSettingsServiceChanged(object sender, AppSettings newSettings)
    {
        try
        {
            Debug.WriteLine($"SettingsViewModel.OnSettingsServiceChanged: invoked.");
            _settings = newSettings ?? new AppSettings();

            // 設定変更時にプロパティ変更通知が必要な場合はここで呼ぶ
            OnPropertyChanged(nameof(UsePresets));
            OnPropertyChanged(nameof(SelectedProcessingMode));
            // 必要に応じて他も通知
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsViewModel.OnSettingsServiceChanged: error: {ex}");
        }
    }

    private IList<ProcessingMode> _processingModes;
    public IList<ProcessingMode> ProcessingModes => _processingModes;

    public IList<CultureInfo> AvailableCultures { get; }

    public ObservableCollection<ProcessingPreset> AvailablePresets { get; }

    private bool _usePresets;
    public bool UsePresets
    {
        get => _usePresets;
        set
        {
            if (_usePresets != value)
            {
                _usePresets = value;
                _settings.UsePresets = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedPreset));
                OnPropertyChanged(nameof(SelectedProcessingMode));
            }
        }
    }

    private ProcessingPreset? _selectedPreset;
    public ProcessingPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (_selectedPreset?.Id != value?.Id)
            {
                _selectedPreset = value;
                OnPropertyChanged();
            }
        }
    }

    private ProcessingPreset? _selectedPresetForTrayClick;
    public ProcessingPreset? SelectedPresetForTrayClick
    {
        get => _selectedPresetForTrayClick;
        set
        {
            var oldId = _selectedPresetForTrayClick?.Id;
            var newId = value?.Id;
            if (oldId != newId)
            {
                Debug.WriteLine($"SelectedPresetForTrayClick: changing from {oldId?.ToString() ?? "null"} -> {newId?.ToString() ?? "null"}");
                _selectedPresetForTrayClick = value;

                if (!_isRefreshingPresets)
                {
                    _settings.SelectedPresetId = value?.Id;
                }

                OnPropertyChanged();
            }
        }
    }

    public ProcessingMode SelectedProcessingMode
    {
        get => _settings.ClipboardProcessingMode;
        set
        {
            if (_settings.ClipboardProcessingMode != value)
            {
                _settings.ClipboardProcessingMode = value;
                OnPropertyChanged();
            }
        }
    }

    private CultureInfo _selectedCulture;
    public CultureInfo SelectedCulture
    {
        get => _selectedCulture;
        set
        {
            if (value == null) return;
            if (_selectedCulture?.Name != value.Name)
            {
                _selectedCulture = value;
                ApplyCulture(value);
                _settings.CultureName = value.Name;
                OnPropertyChanged();
            }
        }
    }

    private void ApplyCulture(CultureInfo ci)
    {
        if (ci == null) return;

        try
        {
            var currentMode = SelectedProcessingMode;

            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;

            LocalizedStrings.Instance.ChangeCulture(ci);

            _processingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();
            OnPropertyChanged(nameof(ProcessingModes));

            SelectedProcessingMode = currentMode;

            if (AvailablePresets != null)
            {
                RefreshPresetLocalization();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyCulture: error: {ex}");
        }
    }

    private void RefreshPresetLocalization()
    {
        if (_presetService == null || AvailablePresets == null) return;

        _isRefreshingPresets = true;

        try
        {
            var savedTrayPresetId = _settings.SelectedPresetId ?? _selectedPresetForTrayClick?.Id; // バグ修正: _selectedPresetではなく_selectedPresetForTrayClickを見るべき可能性が高いが、元のコードを尊重

            _presetService.LoadPresets();

            var selectedId = _selectedPreset?.Id;

            AvailablePresets.Clear();
            foreach (var preset in _presetService.Presets)
            {
                AvailablePresets.Add(preset);
            }

            if (selectedId.HasValue)
            {
                SelectedPreset = AvailablePresets.FirstOrDefault(p => p.Id == selectedId.Value);
            }
            else
            {
                SelectedPreset = AvailablePresets.FirstOrDefault();
            }

            if (savedTrayPresetId.HasValue)
            {
                var tray = AvailablePresets.FirstOrDefault(p => p.Id == savedTrayPresetId.Value);
                if (tray != null)
                {
                    SelectedPresetForTrayClick = tray;
                }
                else
                {
                    SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault();
                }

                _settings.SelectedPresetId = tray?.Id ?? SelectedPresetForTrayClick?.Id;
            }
        }
        finally
        {
            _isRefreshingPresets = false;
        }
    }

    // --- Notification Properties ---
    public int NotificationOffsetX
    {
        get => _settings.NotificationOffsetX;
        set { if (_settings.NotificationOffsetX != value) { _settings.NotificationOffsetX = value; OnPropertyChanged(); } }
    }

    public int NotificationOffsetY
    {
        get => _settings.NotificationOffsetY;
        set { if (_settings.NotificationOffsetY != value) { _settings.NotificationOffsetY = value; OnPropertyChanged(); } }
    }

    public int NotificationMargin
    {
        get => _settings.NotificationMargin;
        set { if (_settings.NotificationMargin != value) { _settings.NotificationMargin = value; OnPropertyChanged(); } }
    }

    public double NotificationMinWidth
    {
        get => _settings.NotificationMinWidth;
        set { if (Math.Abs(_settings.NotificationMinWidth - value) > double.Epsilon) { _settings.NotificationMinWidth = value; OnPropertyChanged(); } }
    }

    public double NotificationMaxWidth
    {
        get => _settings.NotificationMaxWidth;
        set { if (Math.Abs(_settings.NotificationMaxWidth - value) > double.Epsilon) { _settings.NotificationMaxWidth = value; OnPropertyChanged(); } }
    }

    public double NotificationMinHeight
    {
        get => _settings.NotificationMinHeight;
        set { if (Math.Abs(_settings.NotificationMinHeight - value) > double.Epsilon) { _settings.NotificationMinHeight = value; OnPropertyChanged(); } }
    }

    public double NotificationMaxHeight
    {
        get => _settings.NotificationMaxHeight;
        set { if (Math.Abs(_settings.NotificationMaxHeight - value) > double.Epsilon) { _settings.NotificationMaxHeight = value; OnPropertyChanged(); } }
    }

    public int NotificationDelay
    {
        get => _settings.NotificationDelay;
        set { if (_settings.NotificationDelay != value) { _settings.NotificationDelay = value; OnPropertyChanged(); } }
    }

    public bool ShowCopyNotification
    {
        get => _settings.ShowCopyNotification;
        set { if (_settings.ShowCopyNotification != value) { _settings.ShowCopyNotification = value; OnPropertyChanged(); } }
    }

    public bool ShowOperationNotification
    {
        get => _settings.ShowOperationNotification;
        set { if (_settings.ShowOperationNotification != value) { _settings.ShowOperationNotification = value; OnPropertyChanged(); } }
    }

    // --- Preset Helpers ---
    public ProcessingPreset CreatePreset(string name, string description, List<ProcessingStep> steps)
    {
        var preset = _presetService.CreatePreset(name, description, steps);
        AvailablePresets.Add(preset);
        SelectedPreset = preset;
        return preset;
    }

    public bool UpdatePreset(ProcessingPreset preset)
    {
        if (!_presetService.UpdatePreset(preset)) return false;

        var existing = AvailablePresets.FirstOrDefault(p => p.Id == preset.Id);
        if (existing != null)
        {
            var index = AvailablePresets.IndexOf(existing);
            AvailablePresets[index] = preset;
            SelectedPreset = preset;
        }
        return true;
    }

    public bool DeletePreset(System.Guid id)
    {
        if (!_presetService.DeletePreset(id)) return false;

        var preset = AvailablePresets.FirstOrDefault(p => p.Id == id);
        if (preset != null)
        {
            AvailablePresets.Remove(preset);
            SelectedPreset = AvailablePresets.FirstOrDefault();
        }
        return true;
    }

    public void Save()
    {
        try
        {
            Debug.WriteLine($"{nameof(SettingsViewModel)}.{nameof(Save)}: start.");
            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{nameof(SettingsViewModel)}.{nameof(Save)}: error: {ex}");
        }
    }

    /// <summary>
    /// 更新をチェックします
    /// </summary>
    private async Task CheckForUpdatesAsync()
    {
        // ■ MSIXの場合は実行しない（二重チェック）
        if (IsMsix)
        {
            Debug.WriteLine("CheckForUpdatesAsync: Skipped because app is running as MSIX.");
            return;
        }

        if (IsCheckingForUpdates)
        {
            return;
        }

        IsCheckingForUpdates = true;

        try
        {
            Debug.WriteLine("SettingsViewModel: Checking for updates...");
            var updateInfo = await _updateCheckService.CheckForUpdatesAsync();

            if (updateInfo == null)
            {
                System.Windows.MessageBox.Show(
                    LocalizedStrings.Instance.UpdateCheckFailedMessageText,
                    LocalizedStrings.Instance.UpdateCheckFailedText,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (updateInfo.IsUpdateAvailable)
            {
                var message = string.Format(
                    LocalizedStrings.Instance.UpdateAvailableMessageText,
                    updateInfo.LatestVersion,
                    updateInfo.CurrentVersion);

                var result = System.Windows.MessageBox.Show(
                    message,
                    LocalizedStrings.Instance.UpdateAvailableText,
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _updateCheckService.OpenReleasePage(updateInfo.ReleaseUrl);
                }
            }
            else
            {
                var message = string.Format(
                    LocalizedStrings.Instance.NoUpdateAvailableMessageText,
                    updateInfo.CurrentVersion);

                System.Windows.MessageBox.Show(
                    message,
                    LocalizedStrings.Instance.NoUpdateAvailableText,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsViewModel.CheckForUpdatesAsync: error: {ex}");
            FileLogger.LogException(ex, "SettingsViewModel.CheckForUpdatesAsync");

            System.Windows.MessageBox.Show(
                LocalizedStrings.Instance.UpdateCheckFailedMessageText,
                LocalizedStrings.Instance.UpdateCheckFailedText,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}