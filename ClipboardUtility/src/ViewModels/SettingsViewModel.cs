using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ClipboardUtility.src.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    private AppSettings _settings;
    public event PropertyChangedEventHandler PropertyChanged;

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
            CultureName = settings.CultureName
        };

        _processingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();
        SelectedProcessingMode = _settings.ClipboardProcessingMode;

        // 利用可能なカルチャ一覧（必要に応じて追加）
        AvailableCultures = new List<CultureInfo> { new("en-US"), new("ja-JP") };
        // 初期選択
        SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Name == (_settings.CultureName ?? CultureInfo.CurrentUICulture.Name))
                          ?? CultureInfo.CurrentUICulture;
    }

    private IList<ProcessingMode> _processingModes;
    public IList<ProcessingMode> ProcessingModes => _processingModes;
    
    public IList<CultureInfo> AvailableCultures { get; }

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
        CultureName = _settings.CultureName
    };

    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
