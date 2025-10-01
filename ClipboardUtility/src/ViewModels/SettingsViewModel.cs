using ClipboardUtility.src.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Helpers;

namespace ClipboardUtility.src.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    private AppSettings _settings;

    public event PropertyChangedEventHandler PropertyChanged;

    // 引数ありコンストラクタのみ提供（明示的な依存注入）
    public SettingsViewModel(AppSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        // Work on a copy so Cancel can discard changes
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
            ShowCopyNotification = settings.ShowCopyNotification,
            ShowOperationNotification = settings.ShowOperationNotification
        };

        ProcessingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();
        SelectedProcessingMode = _settings.ClipboardProcessingMode;

        SelectModeCommand = new RelayCommand(param =>
        {
            if (param is ProcessingMode pm)
            {
                SelectedProcessingMode = pm;
            }
            else if (param != null)
            {
                if (Enum.TryParse(typeof(ProcessingMode), param.ToString(), out var parsed) && parsed is ProcessingMode parsedMode)
                {
                    SelectedProcessingMode = parsedMode;
                }
            }
        });

        SettingsService.Instance.SettingsChanged += (s, newSettings) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => ReloadFrom(newSettings));
        };
    }

    public IList<ProcessingMode> ProcessingModes { get; }

    public ICommand SelectModeCommand { get; }

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

    public void ReloadFromCurrent()
    {
        ReloadFrom(SettingsService.Instance.Current);
    }

    private void ReloadFrom(AppSettings source)
    {
        if (source == null) return;

        _settings = new AppSettings
        {
            ClipboardProcessingMode = source.ClipboardProcessingMode,
            NotificationOffsetX = source.NotificationOffsetX,
            NotificationOffsetY = source.NotificationOffsetY,
            NotificationMargin = source.NotificationMargin,
            NotificationMinWidth = source.NotificationMinWidth,
            NotificationMaxWidth = source.NotificationMaxWidth,
            NotificationMinHeight = source.NotificationMinHeight,
            NotificationMaxHeight = source.NotificationMaxHeight,
            ShowCopyNotification = source.ShowCopyNotification,
            ShowOperationNotification = source.ShowOperationNotification
        };

        OnPropertyChanged(nameof(SelectedProcessingMode));
        OnPropertyChanged(nameof(NotificationOffsetX));
        OnPropertyChanged(nameof(NotificationOffsetY));
        OnPropertyChanged(nameof(NotificationMargin));
        OnPropertyChanged(nameof(NotificationMinWidth));
        OnPropertyChanged(nameof(NotificationMaxWidth));
        OnPropertyChanged(nameof(NotificationMinHeight));
        OnPropertyChanged(nameof(NotificationMaxHeight));
        OnPropertyChanged(nameof(ShowCopyNotification));
        OnPropertyChanged(nameof(ShowOperationNotification));
    }

    public AppSettings GetSettingsCopy() => new AppSettings
    {
        ClipboardProcessingMode = _settings.ClipboardProcessingMode,
        NotificationOffsetX = _settings.NotificationOffsetX,
        NotificationOffsetY = _settings.NotificationOffsetY,
        NotificationMargin = _settings.NotificationMargin,
        NotificationMinWidth = _settings.NotificationMinWidth,
        NotificationMaxWidth = _settings.NotificationMaxWidth,
        NotificationMinHeight = _settings.NotificationMinHeight,
        NotificationMaxHeight = _settings.NotificationMaxHeight,
        ShowCopyNotification = _settings.ShowCopyNotification,
        ShowOperationNotification = _settings.ShowOperationNotification,
    };

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
