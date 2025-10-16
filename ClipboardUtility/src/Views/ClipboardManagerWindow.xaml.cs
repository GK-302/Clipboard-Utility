using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipboardUtility.src.Views;

public partial class ClipboardManagerWindow : Window
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private bool _isClosing = false;

    public ClipboardManagerWindow()
    {
        InitializeComponent();
        var viewModel = new ViewModels.ClipboardManagerViewModel();
        viewModel.OwnerWindow = this;
        DataContext = viewModel;
        
        // �E�B���h�E���ǂݍ��܂ꂽ��Ɉʒu��ݒ�
        Loaded += ClipboardManagerWindow_Loaded;
        Deactivated += ClipboardManagerWindow_Deactivated;
        Closing += ClipboardManagerWindow_Closing;
    }

    private void ClipboardManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionNearTaskTray();
    }

    private void ClipboardManagerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _isClosing = true;
    }

    private void ClipboardManagerWindow_Deactivated(object sender, EventArgs e)
    {
        // �E�B���h�E�����ɕ��Ă���Œ��̏ꍇ�͉������Ȃ�
        if (_isClosing)
        {
            return;
        }

        // ���[�_���_�C�A���O�i�ݒ�E�B���h�E�Ȃǁj���J���Ă���ꍇ�͕��Ȃ�
        if (OwnedWindows.Count > 0)
        {
            return;
        }

        // �E�B���h�E����A�N�e�B�u�ɂȂ��������
        _isClosing = true;
        Close();
    }

    /// <summary>
    /// �^�X�N�g���C�A�C�R���̋߂��ɃE�B���h�E��z�u���܂�
    /// </summary>
    private void PositionNearTaskTray()
    {
        // ��Ɨ̈�i�^�X�N�o�[����������ʗ̈�j���擾
        var workArea = SystemParameters.WorkArea;
        
        // �E�B���h�E�̃T�C�Y���擾
        double windowWidth = this.ActualWidth;
        double windowHeight = this.ActualHeight;
        
        // ��ʉE��������ɔz�u�i�^�X�N�o�[�����ɂ���ꍇ�j
        // �}�[�W����10�s�N�Z���ݒ�
        double margin = 10;
        
        // �f�t�H���g�͉E����
        double left = workArea.Right - windowWidth - margin;
        double top = workArea.Bottom - windowHeight - margin;
        
        // �^�X�N�o�[�̈ʒu�����o���Ē���
        // ��ʑS�̂̃T�C�Y�ƍ�Ɨ̈���r���ă^�X�N�o�[�̈ʒu�𔻒�
        var screenBounds = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        
        if (workArea.Left > screenBounds.Left)
        {
            // �^�X�N�o�[�������ɂ���
            left = workArea.Left + margin;
            top = workArea.Bottom - windowHeight - margin;
        }
        else if (workArea.Top > screenBounds.Top)
        {
            // �^�X�N�o�[���㑤�ɂ���
            left = workArea.Right - windowWidth - margin;
            top = workArea.Top + margin;
        }
        else if (workArea.Right < screenBounds.Right)
        {
            // �^�X�N�o�[���E���ɂ���
            left = workArea.Right - windowWidth - margin;
            top = workArea.Bottom - windowHeight - margin;
        }
        // �f�t�H���g�i�^�X�N�o�[�������j�̏ꍇ�͊��ɐݒ�ς�
        
        this.Left = left;
        this.Top = top;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

}
