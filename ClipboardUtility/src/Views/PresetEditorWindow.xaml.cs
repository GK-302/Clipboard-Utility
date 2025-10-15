using ClipboardUtility.src.ViewModels;
using ClipboardUtility.src.Models;
using System.Windows;

namespace ClipboardUtility.src.Views
{
    public partial class PresetEditorWindow : Window
    {
        private readonly PresetEditorViewModel _vm;

        public PresetEditorWindow(ProcessingPreset preset)
        {
            InitializeComponent();
            _vm = new PresetEditorViewModel(preset);
            DataContext = _vm;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public ProcessingPreset? GetEditedPreset()
        {
            if (DialogResult == true)
            {
                return _vm.GetResultingPreset();
            }
            return null;
        }
    }
}
