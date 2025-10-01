using ClipboardUtility.src.Models;
using ClipboardUtility.src.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;

namespace ClipboardUtility.src.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;
        private readonly AppSettings _original;

        internal SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();
            _original = currentSettings;
            _vm = new SettingsViewModel(currentSettings);
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