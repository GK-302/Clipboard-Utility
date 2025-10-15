using ClipboardUtility.src.Models;
using ClipboardUtility.src.ViewModels;
using System.Diagnostics;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LicenseWindow licenseWindow = new LicenseWindow();
            licenseWindow.Owner = this; // モーダルウィンドウの所有者を設定
            licenseWindow.ShowDialog(); // モーダルウィンドウとして表示


        }

        private void BtnNewPreset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                // Create an empty preset and open editor
                var preset = new ProcessingPreset { Name = "New Preset", Description = string.Empty };
                var editor = new PresetEditorWindow(preset);
                if (editor.ShowDialog() == true)
                {
                    var edited = editor.GetEditedPreset();
                    if (edited != null)
                    {
                        vm.CreatePreset(edited.Name, edited.Description, edited.Steps);
                        // refresh list
                        OnPresetsChanged(vm);
                    }
                }
            }
        }

        private void BtnEditPreset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                var selected = vm.SelectedPreset;
                if (selected == null) return;

                var editor = new PresetEditorWindow(selected);
                if (editor.ShowDialog() == true)
                {
                    var edited = editor.GetEditedPreset();
                    if (edited != null)
                    {
                        // update existing preset
                        edited.Id = selected.Id; // preserve ID
                        vm.UpdatePreset(edited);
                        OnPresetsChanged(vm);
                    }
                }
            }
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                var selected = vm.SelectedPreset;
                if (selected == null) return;
                if (selected.IsBuiltIn)
                {
                    MessageBox.Show("Built-in preset cannot be deleted.");
                    return;
                }

                var result = MessageBox.Show($"Delete preset '{selected.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    vm.DeletePreset(selected.Id);
                    OnPresetsChanged(vm);
                }
            }
        }

        private void OnPresetsChanged(SettingsViewModel vm)
        {
            // force reload from vm's PresetManager via SettingsViewModel's properties
            // SettingsViewModel exposes AvailablePresets via PresetManager, but UI binding needs update
            // Raise property changed for AvailablePresets by reassigning DataContext to itself (simple workaround)
            var ctx = DataContext;
            DataContext = null;
            DataContext = ctx;
        }
    }
}