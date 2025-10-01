﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Helpers;

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
            ShowCopyNotification = settings.ShowCopyNotification,
            ShowOperationNotification = settings.ShowOperationNotification,
            CultureName = settings.CultureName
        };

        ProcessingModes = Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();
        SelectedProcessingMode = _settings.ClipboardProcessingMode;

        // 利用可能なカルチャ一覧（必要に応じて追加）
        AvailableCultures = new List<CultureInfo> { new CultureInfo("en-US"), new CultureInfo("ja-JP") };
        // 初期選択
        SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Name == (_settings.CultureName ?? CultureInfo.CurrentUICulture.Name)) 
                          ?? CultureInfo.CurrentUICulture;
    }

    public IList<ProcessingMode> ProcessingModes { get; }
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
        // プロセス/スレッド全体の既定カルチャを設定
        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;
        CultureInfo.CurrentCulture = ci;
        CultureInfo.CurrentUICulture = ci;

        // LocalizedStrings に通知してバインド済みのラベルを更新
        LocalizedStrings.Instance.ChangeCulture(ci);
    }

    // Save では既存の GetSettingsCopy を使って SettingsService に保存する（CultureName を含める）
    public void Save()
    {
        SettingsService.Instance.Save(GetSettingsCopy());
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
        CultureName = _settings.CultureName
    };

    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
