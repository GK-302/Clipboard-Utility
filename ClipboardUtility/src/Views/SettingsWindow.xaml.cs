using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using System.Linq;

namespace ClipboardUtility.src.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;
        private readonly IAppRestartService _restartService;
        private readonly SettingsService _settingsService;
        private readonly PresetService _presetService;
        private readonly TextProcessingService _textProcessingService;
        private readonly string _initialCultureName;

        internal SettingsWindow(
                        AppSettings currentSettings,
                        IAppRestartService restartService,
                        ICultureProvider provider,
                        SettingsService settingsService,
                        PresetService presetService,
                        TextProcessingService textProcessingService)
        {
            if (currentSettings == null) throw new ArgumentNullException(nameof(currentSettings));

            InitializeComponent();
            _restartService = restartService;
            _settingsService = settingsService;
            _presetService = presetService;
            _textProcessingService = textProcessingService;

            var settingsToUse = currentSettings;



            _vm = new SettingsViewModel(
                            settingsToUse,
                            provider,
                            _settingsService,
                            _presetService,
                            _textProcessingService);

            DataContext = _vm;

            // ウィンドウ作成時のカルチャ名を保存（null 安全）
            _initialCultureName = _settingsService.Current?.CultureName ?? System.Globalization.CultureInfo.CurrentUICulture.Name;

            // ViewModel が初期化した SelectedPresetForTrayClick を UI に反映
            // Loaded イベントで確実に設定されるようにする
            Loaded += (s, e) =>
            {
                Debug.WriteLine($"SettingsWindow.Loaded: AvailablePresets.Count = {_vm.AvailablePresets.Count}");
                Debug.WriteLine($"SettingsWindow.Loaded: SelectedPresetForTrayClick = {_vm.SelectedPresetForTrayClick?.Name ?? "null"}");
                Debug.WriteLine($"SettingsWindow.Loaded: SelectedPresetForTrayClick.Id = {_vm.SelectedPresetForTrayClick?.Id.ToString() ?? "null"}");
                
                if (_vm.SelectedPresetForTrayClick != null)
                {
                    // Find the index of the selected preset
                    var index = _vm.AvailablePresets.ToList().FindIndex(p => p.Id == _vm.SelectedPresetForTrayClick.Id);
                    Debug.WriteLine($"SettingsWindow.Loaded: Found preset at index = {index}");
                    
                    if (index >= 0)
                    {
                        presetCombobox.SelectedIndex = index;
                        Debug.WriteLine($"SettingsWindow.Loaded: Set presetCombobox.SelectedIndex = {index}");
                        Debug.WriteLine($"SettingsWindow.Loaded: presetCombobox.SelectedItem = {((ProcessingPreset)presetCombobox.SelectedItem)?.Name ?? "null"}");
                    }
                    else
                    {
                        Debug.WriteLine("SettingsWindow.Loaded: WARNING - Preset not found in AvailablePresets, setting index 0 as fallback");
                        presetCombobox.SelectedIndex = 0;
                    }
                }
                else
                {
                    Debug.WriteLine("SettingsWindow.Loaded: WARNING - SelectedPresetForTrayClick is null, setting index 0");
                    presetCombobox.SelectedIndex = 0;
                }
            };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {   
                Debug.WriteLine($"SettingsWindow.BtnSave_Click: invoking ViewModel.Save() {DataContext.ToString()}");

                // 保存前に現在選択カルチャを確認
                var newCulture = vm.SelectedCulture?.Name ?? _settingsService.Current?.CultureName;
                var cultureChanged = !string.Equals(_initialCultureName, newCulture, StringComparison.OrdinalIgnoreCase);

                // まず設定を保存
                vm.Save();

                // カルチャが変わっていれば再起動確認ダイアログを表示
                if (cultureChanged)
                {
                    var result = MessageBox.Show(
                        @"言語を変更しました。アプリを再起動しますか？
                    （再起動しないと一部表示が反映されない場合があります）",
                        "再起動の確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // 保存後、言語が変わっていてユーザが再起動を同意したとき:
                            _restartService.Restart(); // AppRestartService 内で Shutdown します
                            return; // Restart がアプリを終了するためここで戻す
                        }
                        catch (Exception ex)
                        {
                            FileLogger.LogException(ex, "SettingsWindow.BtnSave_Click: Restart failed");
                            MessageBox.Show("アプリの再起動に失敗しました。手動で再起動してください。", "再起動失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                            // 再起動失敗でもウィンドウは閉じてよい（設定は保存済み）
                        }
                    }
                }
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LicenseWindow licenseWindow = new LicenseWindow();
            licenseWindow.Owner = this;
            licenseWindow.ShowDialog();
        }

        private void BtnNewPreset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                // Create an empty preset and open editor
                var preset = new ProcessingPreset { Name = "New Preset", Description = string.Empty };
                var editor = new PresetEditorWindow(preset) { Owner = this };
                
                if (editor.ShowDialog() == true)
                {
                    var edited = editor.GetEditedPreset();
                    if (edited != null)
                    {
                        vm.CreatePreset(edited.Name, edited.Description, edited.Steps);
                        // ObservableCollection なので自動的に UI が更新される
                    }
                }
            }
        }

        private void BtnEditPreset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                var selected = vm.SelectedPreset;
                if (selected == null)
                {
                    MessageBox.Show("Please select a preset to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (selected.IsBuiltIn)
                {
                    MessageBox.Show("Built-in presets cannot be edited.", "Cannot Edit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var editor = new PresetEditorWindow(selected) { Owner = this };
                
                if (editor.ShowDialog() == true)
                {
                    var edited = editor.GetEditedPreset();
                    if (edited != null)
                    {
                        // preserve ID
                        edited.Id = selected.Id;
                        vm.UpdatePreset(edited);
                        // ObservableCollection なので自動的に UI が更新される
                    }
                }
            }
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                var selected = vm.SelectedPreset;
                if (selected == null)
                {
                    MessageBox.Show("Please select a preset to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (selected.IsBuiltIn)
                {
                    MessageBox.Show("Built-in presets cannot be deleted.", "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to delete the preset '{selected.Name}'?", 
                    "Confirm Delete", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    vm.DeletePreset(selected.Id);
                }
            }
        }
    }
}
