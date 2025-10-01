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

        // �����I�� AppSettings ��v������R���X�g���N�^�̂ݒ񋟂���i�݊����͕s�v�j
        internal SettingsWindow(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            InitializeComponent();

            // �K�v�Ȃ�ŐV�̃����^�C���ݒ�Ƀt�H�[���o�b�N���邪�A
            // �Ăяo�����͓K�؂� settings ��n�����Ƃ����҂���
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