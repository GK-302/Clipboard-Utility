using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ClipboardUtility.src.ViewModels
{
    internal class WelcomeWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public WelcomeWindowViewModel()
        {
            // 利用可能なカルチャ一覧
            AvailableCultures = new List<CultureInfo> { new("en-US"), new("ja-JP") };

            // 現在の設定からカルチャを取得
            var settings = SettingsService.Instance.Current;
            var cultureName = settings?.CultureName ?? CultureInfo.CurrentUICulture.Name;
            
            // 初期選択
            _selectedCulture = AvailableCultures.FirstOrDefault(c => c.Name == cultureName)
                              ?? CultureInfo.CurrentUICulture;


            // 設定変更の監視
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

        private void SaveCultureSetting(CultureInfo ci)
        {
            try
            {
                // 現在の設定を取得
                var settings = SettingsService.Instance.Current ?? new AppSettings();
                
                // カルチャ名を更新
                settings.CultureName = ci.Name;
                
                // 設定を保存
                SettingsService.Instance.Save(settings);
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "WelcomeWindowViewModel.SaveCultureSetting");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}