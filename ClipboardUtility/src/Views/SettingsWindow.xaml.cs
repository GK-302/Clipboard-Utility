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

        internal SettingsWindow(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            InitializeComponent();

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
                    // ObservableCollection なので自動的に UI が更新される
                }
            }
        }
    }
}