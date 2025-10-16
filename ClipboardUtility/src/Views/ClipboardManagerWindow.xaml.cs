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
        
        // ウィンドウが読み込まれた後に位置を設定
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
        // ウィンドウが既に閉じている最中の場合は何もしない
        if (_isClosing)
        {
            return;
        }

        // モーダルダイアログ（設定ウィンドウなど）が開いている場合は閉じない
        if (OwnedWindows.Count > 0)
        {
            return;
        }

        // ウィンドウが非アクティブになったら閉じる
        _isClosing = true;
        Close();
    }

    /// <summary>
    /// タスクトレイアイコンの近くにウィンドウを配置します
    /// </summary>
    private void PositionNearTaskTray()
    {
        // 作業領域（タスクバーを除いた画面領域）を取得
        var workArea = SystemParameters.WorkArea;
        
        // ウィンドウのサイズを取得
        double windowWidth = this.ActualWidth;
        double windowHeight = this.ActualHeight;
        
        // 画面右下隅を基準に配置（タスクバーが下にある場合）
        // マージンを10ピクセル設定
        double margin = 10;
        
        // デフォルトは右下隅
        double left = workArea.Right - windowWidth - margin;
        double top = workArea.Bottom - windowHeight - margin;
        
        // タスクバーの位置を検出して調整
        // 画面全体のサイズと作業領域を比較してタスクバーの位置を判定
        var screenBounds = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        
        if (workArea.Left > screenBounds.Left)
        {
            // タスクバーが左側にある
            left = workArea.Left + margin;
            top = workArea.Bottom - windowHeight - margin;
        }
        else if (workArea.Top > screenBounds.Top)
        {
            // タスクバーが上側にある
            left = workArea.Right - windowWidth - margin;
            top = workArea.Top + margin;
        }
        else if (workArea.Right < screenBounds.Right)
        {
            // タスクバーが右側にある
            left = workArea.Right - windowWidth - margin;
            top = workArea.Bottom - windowHeight - margin;
        }
        // デフォルト（タスクバーが下側）の場合は既に設定済み
        
        this.Left = left;
        this.Top = top;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

}
