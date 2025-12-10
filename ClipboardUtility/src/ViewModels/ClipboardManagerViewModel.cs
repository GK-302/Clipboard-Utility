using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ClipboardUtility.src.ViewModels;

public class ClipboardManagerViewModel : INotifyPropertyChanged
{
    private readonly ClipboardService _clipboardService;
    private readonly TextProcessingService _textProcessingService;
    private readonly PresetService _presetService;
    private readonly SettingsService _settingsService;
    private readonly IAppRestartService _restartService;
    private readonly ICultureProvider _cultureProvider;
    private readonly UpdateCheckService _updateCheckService;
    private string _clipboardText = string.Empty;
    private ProcessingMode _selectedProcessingMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    // 親ウィンドウへの参照（設定ウィンドウのオーナー指定用）
    public System.Windows.Window? OwnerWindow { get; set; }

    public ClipboardManagerViewModel(
            ClipboardService clipboardService,
            TextProcessingService textProcessingService,
            PresetService presetService,
            SettingsService settingsService,
            IAppRestartService restartService,
            ICultureProvider cultureProvider,
            UpdateCheckService updateCheckService)
    {
        //_clipboardService = new ClipboardService();
        //_textProcessingService = new TextProcessingService();

        //// PresetService を初期化してプリセットを読み込む
        //_presetService = new PresetService(_textProcessingService);
        //_presetService.LoadPresets();
        _clipboardService = clipboardService;
        _textProcessingService = textProcessingService;
        _presetService = presetService;
        _settingsService = settingsService;
        _restartService = restartService;
        _cultureProvider = cultureProvider;
        _updateCheckService = updateCheckService;

        Presets = new ObservableCollection<ProcessingPreset>(_presetService.Presets);
        SelectedPreset = Presets.FirstOrDefault();

        // 利用可能な処理モードを設定（すべての列挙値を自動取得）
        ProcessingModes = new ObservableCollection<ProcessingMode>(
            Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>()
        );

        SelectedProcessingMode = ProcessingMode.None;

        // コマンドの初期化
        ApplyProcessingCommand = new RelayCommand(_ => ExecuteApplyProcessing());
        ApplyPresetCommand = new RelayCommand(_ => ExecuteApplyPreset());
        CopyToClipboardCommand = new RelayCommand(_ => ExecuteCopyToClipboard());
        RefreshClipboardCommand = new RelayCommand(_ => ExecuteRefreshClipboard());
        ClearCommand = new RelayCommand(_ => ExecuteClear());
        OpenSettingsCommand = new RelayCommand(_ => ExecuteOpenSettings());
        ExitApplicationCommand = new RelayCommand(_ => ExecuteExitApplication());

        // 初期クリップボード読み込み
        LoadClipboardContent();
    }

    public string ClipboardText
    {
        get => _clipboardText;
        set
        {
            if (_clipboardText != value)
            {
                _clipboardText = value;
                OnPropertyChanged();
                UpdateStatistics();
            }
        }
    }

    public int CharacterCount => string.IsNullOrEmpty(ClipboardText) ? 0 : ClipboardText.Length;

    public int WordCount => CountWords(ClipboardText);

    public int LineCount => CountLines(ClipboardText);

    public ProcessingMode SelectedProcessingMode
    {
        get => _selectedProcessingMode;
        set
        {
            if (_selectedProcessingMode != value)
            {
                _selectedProcessingMode = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ProcessingMode> ProcessingModes { get; }

    // Preset support
    public ObservableCollection<ProcessingPreset> Presets { get; }

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

    public ICommand ApplyProcessingCommand { get; }
    public ICommand ApplyPresetCommand { get; }
    public ICommand CopyToClipboardCommand { get; }
    public ICommand RefreshClipboardCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    private void LoadClipboardContent()
    {
        try
        {
            var text = _clipboardService.GetTextSafely();
            ClipboardText = text ?? string.Empty;
            Debug.WriteLine($"ClipboardManagerViewModel: Loaded clipboard content, length={ClipboardText.Length}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardManagerViewModel: Failed to load clipboard: {ex.Message}");
            ClipboardText = string.Empty;
        }
    }

    private void ExecuteApplyProcessing()
    {
        try
        {
            if (SelectedProcessingMode == ProcessingMode.None)
            {
                Debug.WriteLine("ClipboardManagerViewModel: No processing mode selected");
                return;
            }

            var processedText = _textProcessingService.Process(ClipboardText, SelectedProcessingMode);
            ClipboardText = processedText;
            Debug.WriteLine($"ClipboardManagerViewModel: Applied {SelectedProcessingMode}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardManagerViewModel: Failed to apply processing: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardManagerViewModel.ExecuteApplyProcessing");
        }
    }

    private void ExecuteApplyPreset()
    {
        try
        {
            if (SelectedPreset is null)
            {
                Debug.WriteLine("ClipboardManagerViewModel: No preset selected");
                return;
            }

            var result = _presetService.ExecutePreset(SelectedPreset, ClipboardText);
            ClipboardText = result;
            Debug.WriteLine($"ClipboardManagerViewModel: Applied preset {SelectedPreset.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardManagerViewModel: Failed to apply preset: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardManagerViewModel.ExecuteApplyPreset");
        }
    }

    private void ExecuteCopyToClipboard()
    {
        try
        {
            _clipboardService.SetText(ClipboardText);
            Debug.WriteLine("ClipboardManagerViewModel: Copied to clipboard");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardManagerViewModel: Failed to copy to clipboard: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardManagerViewModel.ExecuteCopyToClipboard");
        }
    }

    private void ExecuteRefreshClipboard()
    {
        LoadClipboardContent();
        Debug.WriteLine("ClipboardManagerViewModel: Refreshed clipboard content");
    }

    private void ExecuteClear()
    {
        ClipboardText = string.Empty;
        Debug.WriteLine("ClipboardManagerViewModel: Cleared clipboard text");
    }

    private void ExecuteOpenSettings()
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var currentSettings = _settingsService.Current;
                var settingsWindow = new src.Views.SettingsWindow(
                                                        currentSettings,
                                                        _restartService,
                                                        _cultureProvider,
                                                        _settingsService,
                                                        _presetService,
                                                        _textProcessingService,
                                                        _updateCheckService);

                // 親ウィンドウを設定してモーダルダイアログとして表示
                if (OwnerWindow != null)
                {
                    settingsWindow.Owner = OwnerWindow;
                }
                
                settingsWindow.ShowDialog();

                // 設定画面でプリセットが変更された可能性があるため、プリセットを再読み込みして UI を更新
                _presetService.LoadPresets();
                Presets.Clear();
                foreach (var p in _presetService.Presets)
                {
                    Presets.Add(p);
                }
                SelectedPreset = Presets.FirstOrDefault();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardManagerViewModel: Failed to open settings: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardManagerViewModel.ExecuteOpenSettings");
        }
    }

    private void ExecuteExitApplication()
    {
        try
        {
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardManagerViewModel: Failed to exit application: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardManagerViewModel.ExecuteExitApplication");
        }
    }

    private void UpdateStatistics()
    {
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(WordCount));
        OnPropertyChanged(nameof(LineCount));
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    private int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        int count = lines.Length;
        if (count > 0 && string.IsNullOrEmpty(lines[count - 1]))
        {
            count--;
        }
        return Math.Max(1, count);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
