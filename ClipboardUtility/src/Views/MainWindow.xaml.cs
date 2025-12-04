using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using System; // EventArgsなどで必要
using System.Runtime.InteropServices; // DllImportに必要
using System.Windows;
using System.Windows.Interop; // WindowInteropHelperに必要

namespace ClipboardUtility;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    // 実行時用コンストラクタ
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        this.DataContext = _viewModel;

        this.Loaded += MainWindow_Loaded;
        this.Closing += MainWindow_Closing;
    }

    /// <summary>
    /// ウィンドウのハンドル（HWND）が生成された直後に呼ばれます。
    /// ここでWin32 APIを使用してウィンドウスタイルを書き換えます。
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyToolWindowStyle();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel?.Initialize(this);
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // App.xaml.csでクリーンアップするので、ここでは何もしない
        // または最小化して隠すだけにする
        e.Cancel = true;
        this.WindowState = WindowState.Minimized;

        // 念のためここでも設定（XAMLでFalseになっていればそのままでもOK）
        this.ShowInTaskbar = false;
    }

    // ---------------------------------------------------------
    // Win32 API 関連処理 (Alt + Tab から隠すための実装)
    // ---------------------------------------------------------

    private void ApplyToolWindowStyle()
    {
        var helper = new WindowInteropHelper(this);

        // 現在の拡張ウィンドウスタイルを取得
        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);

        // WS_EX_TOOLWINDOW フラグを追加
        // これにより Alt+Tab スイッチャーから非表示になります
        SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
}