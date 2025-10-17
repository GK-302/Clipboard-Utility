﻿using ClipboardUtility.src.Helpers;
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

    public SettingsViewModel(AppSettings settings)
    {
        _settings = new AppSettings
        {
            ClipboardProcessingMode = settings.ClipboardProcessingMode,
            NotificationOffsetX = settings.NotificationOffsetX,
            NotificationOffsetY = settings.NotificationOffsetY,
            NotificationMargin = settings.NotificationMargin,
            NotificationMinWidth = settings.NotificationMinWidth,
            NotificationMaxWidth = settings.NotificationMaxWidth,
            NotificationMinHeight = settings.NotificationMinHeight,
            NotificationMaxHeight = settings.NotificationMaxHeight,
            NotificationDelay = settings.NotificationDelay,
            ShowCopyNotification = settings.ShowCopyNotification,
            ShowOperationNotification = settings.ShowOperationNotification,
            CultureName = settings.CultureName,
            SelectedPresetId = settings.SelectedPresetId,
            UsePresets = settings.UsePresets
            ,
            RunAtStartup = settings.RunAtStartup
        };

        _processingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();
        SelectedProcessingMode = _settings.ClipboardProcessingMode;

        // 利用可能なカルチャ一覧（必要に応じて追加）
        AvailableCultures = new List<CultureInfo> { new("en-US"), new("ja-JP") };
        
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
            // Default to first loaded preset and persist in the runtime copy of settings so UI and internal state match
            SelectedPresetForTrayClick = AvailablePresets.FirstOrDefault();
            _settings.SelectedPresetId = SelectedPresetForTrayClick?.Id;
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
            if (_selectedPresetForTrayClick?.Id != value?.Id)
            {
                _selectedPresetForTrayClick = value;
                _settings.SelectedPresetId = value?.Id;
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
                _selectedCulture = value;
                // 即時にカルチャを切り替える（UI更新用）
                ApplyCulture(value);
                // ViewModel 内の設定に反映
                _settings.CultureName = value.Name;
                OnPropertyChanged();
            }
        }
    }

    private void ApplyCulture(CultureInfo ci)
    {
        if (ci == null) return;
        
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
    }

    /// <summary>
    /// プリセットのローカライゼーションを更新します（カルチャ変更時）
    /// </summary>
    private void RefreshPresetLocalization()
    {
        // Null チェックを追加
        if (_presetService == null || AvailablePresets == null) return;

        _presetService.LoadPresets();
        
        // 現在の選択を保存
        var selectedId = _selectedPreset?.Id;

        // 既存の ObservableCollection を更新（UI が自動的に反映される）
        AvailablePresets.Clear();
        foreach (var preset in _presetService.Presets)
        {
            AvailablePresets.Add(preset);
        }

        // 選択を復元（ID で検索）
        if (selectedId.HasValue)
        {
            SelectedPreset = AvailablePresets.FirstOrDefault(p => p.Id == selectedId.Value);
        }
        else
        {
            SelectedPreset = AvailablePresets.FirstOrDefault();
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
        SettingsService.Instance.Save(GetSettingsCopy());
    }

    public AppSettings GetSettingsCopy() => new()
    {
        ClipboardProcessingMode = _settings.ClipboardProcessingMode,
        NotificationOffsetX = _settings.NotificationOffsetX,
        NotificationOffsetY = _settings.NotificationOffsetY,
        NotificationMargin = _settings.NotificationMargin,
        NotificationMinWidth = _settings.NotificationMinWidth,
        NotificationMaxWidth = _settings.NotificationMaxWidth,
        NotificationMinHeight = _settings.NotificationMinHeight,
        NotificationMaxHeight = _settings.NotificationMaxHeight,
        NotificationDelay = _settings.NotificationDelay,
        ShowCopyNotification = _settings.ShowCopyNotification,
        ShowOperationNotification = _settings.ShowOperationNotification,
        CultureName = _settings.CultureName,
        SelectedPresetId = _settings.SelectedPresetId,
        UsePresets = _settings.UsePresets
        ,
        RunAtStartup = _settings.RunAtStartup
    };

    public bool RunAtStartup
    {
        get => _settings.RunAtStartup;
        set { if (_settings.RunAtStartup != value) { _settings.RunAtStartup = value; OnPropertyChanged(); } }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
