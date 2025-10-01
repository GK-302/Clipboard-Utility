using ClipboardUtility.src.Models;
using ClipboardUtility.src.ViewModels;
using System;
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
            var edited = _vm.GetSettingsCopy();
            edited.Save();

            _original.ClipboardProcessingMode = edited.ClipboardProcessingMode;
            _original.NotificationOffsetX = edited.NotificationOffsetX;
            _original.NotificationOffsetY = edited.NotificationOffsetY;
            _original.NotificationMargin = edited.NotificationMargin;
            _original.NotificationMinWidth = edited.NotificationMinWidth;
            _original.NotificationMaxWidth = edited.NotificationMaxWidth;
            _original.NotificationMinHeight = edited.NotificationMinHeight;
            _original.NotificationMaxHeight = edited.NotificationMaxHeight;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}