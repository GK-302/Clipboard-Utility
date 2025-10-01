using ClipboardUtility.src.Models;
using ClipboardUtility.src.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using ClipboardUtility.src.Services;

namespace ClipboardUtility.src.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;

        // 明示的に AppSettings を要求するコンストラクタのみ提供する（互換性は不要）
        internal SettingsWindow(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            InitializeComponent();

            // 必要なら最新のランタイム設定にフォールバックするが、
            // 呼び出し側は適切な settings を渡すことを期待する
            var settingsToUse = settings;
            _vm = new SettingsViewModel(settingsToUse);
            DataContext = _vm;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                Debug.WriteLine("SettingsWindow.BtnSave_Click: invoking ViewModel.Save()");
                vm.Save();
            }
            else
            {
                Debug.WriteLine("SettingsWindow.BtnSave_Click: DataContext is not SettingsViewModel");
            }

            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}