using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;

namespace ClipboardUtility.src.ViewModels;

public class WelcomeWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private readonly SettingsService _settingsService;

    public WelcomeWindowViewModel(ICultureProvider cultureProvider, SettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        var available = cultureProvider?.AvailableCultures ?? new List<CultureInfo> { CultureInfo.CurrentUICulture };
        AvailableCultures = available.ToList();

        // 現在の設定からカルチャを取得
        var settings = _settingsService.Current;
        var cultureName = settings?.CultureName ?? CultureInfo.CurrentUICulture.Name;

        // 初期選択
        _selectedCulture = AvailableCultures.FirstOrDefault(c => c.Name == cultureName)
                          ?? CultureInfo.CurrentUICulture;

        LoadAppVersion();
        // 設定変更の監視
    }

    private string _appVersion;
    /// <summary>
    /// アプリケーションのバージョン情報（Viewにバインドされます）
    /// </summary>
    public string AppVersion
    {
        get => _appVersion;
        set
        {
            if (_appVersion != value)
            {
                _appVersion = value;
                OnPropertyChanged(); // 既存の OnPropertyChanged を呼び出します
            }
        }
    }
    public IList<CultureInfo> AvailableCultures { get; }

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
                // 設定に保存
                SaveCultureSetting(value);
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
    /// <summary>
    /// アセンブリからバージョン情報を取得し、AppVersionプロパティにセットします。
    /// </summary>
    private void LoadAppVersion()
    {
        try
        {
            // 現在実行中のアセンブリ（EXE）を取得
            var assembly = Assembly.GetExecutingAssembly();

            // ファイルバージョン情報を取得
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string productVersion = fvi.ProductVersion;
            // $(VersionPrefix) の値が反映されやすい ProductVersion を使用します
            int plusIndex = productVersion.IndexOf('+');
            if (plusIndex > 0) // '+' が見つかり、かつ文字列の先頭ではない場合
            {
                // "1.0.0" の部分だけを抽出
                AppVersion = productVersion.Substring(0, plusIndex);
            }
            else
            {
                // "+" が含まれていない場合はそのまま使用
                AppVersion = productVersion;
            }


            // (もし "Version: 1.2.3" のようにしたい場合は以下を使用)
            // AppVersion = $"Version: {fvi.ProductVersion}";
        }
        catch (Exception ex)
        {
            // 既存のロガーを利用してエラーを記録
            FileLogger.LogException(ex, "WelcomeWindowViewModel.LoadAppVersion");
            AppVersion = "N/A"; // 取得失敗時のフォールバック
        }
    }
    private void SaveCultureSetting(CultureInfo ci)
    {
        try
        {

            // 現在の設定を取得
            var settings = _settingsService.Current ?? new AppSettings();

            // カルチャ名を更新
            settings.CultureName = ci.Name;

            // 設定を保存
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            FileLogger.LogException(ex, "WelcomeWindowViewModel.SaveCultureSetting");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}