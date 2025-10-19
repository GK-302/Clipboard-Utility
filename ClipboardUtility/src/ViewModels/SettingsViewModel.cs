using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ClipboardUtility.src.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    private AppSettings _settings;
    public event PropertyChangedEventHandler PropertyChanged;

    // Preset manager
    private readonly PresetService _presetService;

    // プリセット更新中フラグ（UI が一時的に SelectedItem を null にすることで設定を書き換えないようにする）
    private bool _isRefreshingPresets;

    public SettingsViewModel(AppSettings settings, ICultureProvider cultureProvider)
    {
        // SettingsService の Current オブジェクトの参照を使う（コピーしない）
        _settings = SettingsService.Instance.Current;

        // Subscribe to SettingsService changes so we update our _settings reference
        SettingsService.Instance.SettingsChanged += OnSettingsServiceChanged;
        Debug.WriteLine($"SettingsViewModel: Subscribed to SettingsService.SettingsChanged. Initial Current hash={_settings?.GetHashCode()} SelectedPresetId={_settings?.SelectedPresetId}");

        _processingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();
        SelectedProcessingMode = _settings.ClipboardProcessingMode;

        // 利用可能なカルチャ一覧（必要に応じて追加）
        var available = cultureProvider?.AvailableCultures ?? new List<CultureInfo> { CultureInfo.CurrentUICulture };
        AvailableCultures = available.ToList();
        
        // Preset manager を作成してプリセットを読み込む
        _presetService = new PresetService(new TextProcessingService());
        _presetService.LoadPresets();

        // ObservableCollection に変更して動的更新を可能にする
        AvailablePresets = new ObservableCollection<ProcessingPreset>(_presetService.Presets);

        // 初期選択: 先頭のビルトインプリセットを選択しておく（UI側で変更可能）
        SelectedPreset = AvailablePresets.FirstOrDefault();

        // 初期 UsePresets を反映（UI のチェックボックスと内部設定を一致させる）
        _usePresets = _settings.UsePresets;

        // タスクトレイクリック用のプリセットを復元、なければ最初に読み込んだプリセットを初期値として使う
        if (_settings.SelectedPresetId.HasValue)
        {
            Debug.WriteLine($"SettingsViewModel: Loading preset from settings, ID = {_settings.SelectedPresetId.Value}");
            SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault(p => p.Id == _settings.SelectedPresetId.Value);
            
            if (SelectedPresetForTrayClick != null)
            {
                Debug.WriteLine($"SettingsViewModel: Found preset: {SelectedPresetForTrayClick.Name}");
            }
            else
            {
                Debug.WriteLine("SettingsViewModel: Preset not found, using first preset as fallback");
                SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault();
            }
        }
        else
        {
            Debug.WriteLine("SettingsViewModel: No preset ID in settings, using first preset");
            // Default to first loaded preset and persist in the runtime Current so UI and internal state match
            SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault();
            _settings.SelectedPresetId = SelectedPresetForTrayClick?.Id;
            Debug.WriteLine($"SettingsViewModel: Assigned settings.SelectedPresetId = {_settings.SelectedPresetId} (during ctor fallback)");
        }
        
        Debug.WriteLine($"SettingsViewModel: Final SelectedPresetForTrayClick = {SelectedPresetForTrayClick?.Name ?? "null"}");
        Debug.WriteLine($"SettingsViewModel: Final SelectedPresetForTrayClick.Id = {SelectedPresetForTrayClick?.Id.ToString() ?? "null"}");
        Debug.WriteLine($"SettingsViewModel: AvailablePresets count = {AvailablePresets.Count}");
        for (int i = 0; i < AvailablePresets.Count; i++)
        {
            Debug.WriteLine($"  [{i}] {AvailablePresets[i].Name} (ID: {AvailablePresets[i].Id})");
        }

        // カルチャの初期選択（AvailablePresets 初期化後に行う）
        SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Name == (_settings.CultureName ?? CultureInfo.CurrentUICulture.Name))
                          ?? CultureInfo.CurrentUICulture;
    }

    private void OnSettingsServiceChanged(object sender, AppSettings newSettings)
    {
        try
        {
            Debug.WriteLine($"SettingsViewModel.OnSettingsServiceChanged: invoked. old _settings hash={( _settings?.GetHashCode() ?? 0 )}, newSettings hash={( newSettings?.GetHashCode() ?? 0 )}");
            Debug.WriteLine($"  old SelectedPresetId={_settings?.SelectedPresetId}, new SelectedPresetId={newSettings?.SelectedPresetId}");
            // Update local reference to the canonical runtime settings
            _settings = newSettings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsViewModel.OnSettingsServiceChanged: error: {ex}");
        }
    }

    private IList<ProcessingMode> _processingModes;
    public IList<ProcessingMode> ProcessingModes => _processingModes;
    
    public IList<CultureInfo> AvailableCultures { get; }

    // Presets - ObservableCollection に変更
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
                // Persist selection to settings when toggling
                _settings.UsePresets = value;
                OnPropertyChanged();
                // When switching modes, ensure either preset selection or processing mode is applied
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

    // タスクトレイアイコン左クリック時のアクション用プリセット
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

                // リスト更新中は settings の書き換えを抑止する
                if (!_isRefreshingPresets)
                {
                    _settings.SelectedPresetId = value?.Id;
                    Debug.WriteLine($"SelectedPresetForTrayClick: assigned settings.SelectedPresetId = {_settings.SelectedPresetId} (settings hash={_settings?.GetHashCode()})");
                }
                else
                {
                    Debug.WriteLine("SelectedPresetForTrayClick: suppressed settings.SelectedPresetId write because presets are being refreshed.");
                }

                OnPropertyChanged();
            }
        }
    }

    // 追加: SelectedProcessingMode プロパティ（XAMLバインドと参照用）
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

    // 選択中のカルチャ（UIのComboBoxにバインド）
    private CultureInfo _selectedCulture;
    public CultureInfo SelectedCulture
    {
        get => _selectedCulture;
        set
        {
            if (value == null) return;
            if (_selectedCulture?.Name != value.Name)
            {
                Debug.WriteLine($"SelectedCulture: changing from {_selectedCulture?.Name ?? "null"} -> {value.Name}");
                _selectedCulture = value;
                // 即時にカルチャを切り替える（UI更新用）
                ApplyCulture(value);
                // ViewModel 内の設定に反映
                _settings.CultureName = value.Name;
                Debug.WriteLine($"SelectedCulture: updated settings.CultureName = {_settings.CultureName} (settings hash={_settings?.GetHashCode()})");
                OnPropertyChanged();
            }
        }
    }

    private void ApplyCulture(CultureInfo ci)
    {
        if (ci == null) return;
        
        try
        {
            Debug.WriteLine($"ApplyCulture: start. current SelectedPresetForTrayClick.Id={SelectedPresetForTrayClick?.Id.ToString() ?? "null"}, settings.SelectedPresetId={_settings.SelectedPresetId?.ToString() ?? "null"}");
            // 現在選択されているモードを保存
            var currentMode = SelectedProcessingMode;
            
            // プロセス/スレッド全体の既定カルチャを設定
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;

            // LocalizedStrings に通知してバインド済みのラベルを更新
            LocalizedStrings.Instance.ChangeCulture(ci);

            // ProcessingModesリストを再作成して、ComboBoxを強制的に更新
            _processingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();
            OnPropertyChanged(nameof(ProcessingModes));
            
            // 選択を復元
            SelectedProcessingMode = currentMode;

            // プリセットのローカライゼーションを更新（AvailablePresets が null でない場合のみ）
            if (AvailablePresets != null)
            {
                RefreshPresetLocalization();
            }

            Debug.WriteLine($"ApplyCulture: end. after RefreshPresetLocalization settings.SelectedPresetId={_settings.SelectedPresetId?.ToString() ?? "null"}, SelectedPresetForTrayClick.Id={SelectedPresetForTrayClick?.Id.ToString() ?? "null"}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyCulture: error: {ex}");
        }
    }

    /// <summary>
    /// プリセットのローカライゼーションを更新します（カルチャ変更時）
    /// </summary>
    private void RefreshPresetLocalization()
    {
        if (_presetService == null || AvailablePresets == null) return;

        // 更新中フラグを立てる（UI からの一時的 null 書き込みを抑止）
        _isRefreshingPresets = true;

        try
        {
            Debug.WriteLine($"RefreshPresetLocalization: start. saved selectedPreset.Id = {_selectedPreset?.Id.ToString() ?? "null"}, settings.SelectedPresetId = {_settings.SelectedPresetId?.ToString() ?? "null"}");

            // 退避：トレイ用プリセット ID（UI の一時的クリアがあっても復元できるように）
            var savedTrayPresetId = _settings.SelectedPresetId ?? _selectedPreset?.Id;

            _presetService.LoadPresets();
            
            // 現在の選択を保存
            var selectedId = _selectedPreset?.Id;

            // 既存の ObservableCollection を更新（UI が自動的に反映される）
            AvailablePresets.Clear();
            foreach (var preset in _presetService.Presets)
            {
                AvailablePresets.Add(preset);
            }

            Debug.WriteLine($"RefreshPresetLocalization: loaded presets count={AvailablePresets.Count}. ids = {string.Join(',', AvailablePresets.Select(p => p.Id.ToString()))}");

            // 選択を復元（ID で検索）
            if (selectedId.HasValue)
            {
                SelectedPreset = AvailablePresets.FirstOrDefault(p => p.Id == selectedId.Value);    
                Debug.WriteLine($"RefreshPresetLocalization: attempted to restore SelectedPreset by saved selectedId={selectedId}. result SelectedPreset.Id={SelectedPreset?.Id.ToString() ?? "null"}");
            }
            else
            {
                SelectedPreset = AvailablePresets.FirstOrDefault();
                Debug.WriteLine($"RefreshPresetLocalization: no saved selectedId, fallback SelectedPreset.Id={SelectedPreset?.Id.ToString() ?? "null"}");
            }

            // トレイ用プリセットを復元（保存されている ID 優先）
            if (savedTrayPresetId.HasValue)
            {
                var tray = AvailablePresets.FirstOrDefault(p => p.Id == savedTrayPresetId.Value);
                if (tray != null)
                {
                    SelectedPresetForTrayClick = tray;
                    Debug.WriteLine($"RefreshPresetLocalization: restored SelectedPresetForTrayClick to id={tray.Id}");
                }
                else
                {
                    SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault();
                    Debug.WriteLine($"RefreshPresetLocalization: could not find saved tray id={savedTrayPresetId}, fallback to {SelectedPresetForTrayClick?.Id.ToString() ?? "null"}");
                }

                // この直後は _isRefreshingPresets=true のため settings.SelectedPresetId 書き込みは抑止される。
                // 明示的に settings.SelectedPresetId を保存しておく：
                _settings.SelectedPresetId = tray?.Id ?? SelectedPresetForTrayClick?.Id;
                Debug.WriteLine($"RefreshPresetLocalization: explicitly set settings.SelectedPresetId = {_settings.SelectedPresetId?.ToString() ?? "null"}");
            }
        }
        finally
        {
            _isRefreshingPresets = false;
        }
    }

    // Notification size/offset props
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

    // Add missing boolean properties for bindings
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

    // Preset management helper wrappers - ObservableCollection を直接操作
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

        // ObservableCollection 内の既存アイテムを更新
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

    // Called by the view to persist changes
    public void Save()
    {
        try
        {
            Debug.WriteLine($"{nameof(SettingsViewModel)}.{nameof(Save)}: start. _settings hash={_settings?.GetHashCode()} settings.SelectedPresetId={_settings?.SelectedPresetId?.ToString() ?? "null"}");
            // Current の参照をそのまま保存する
            SettingsService.Instance.Save(_settings);
            Debug.WriteLine($"{nameof(SettingsViewModel)}.{nameof(Save)}: after SettingsService.Save. settings.SelectedPresetId={_settings?.SelectedPresetId?.ToString() ?? "null"}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{nameof(SettingsViewModel)}.{nameof(Save)}: error: {ex}");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
