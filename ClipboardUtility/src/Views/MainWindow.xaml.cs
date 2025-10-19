using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using System.Windows;

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
        this.ShowInTaskbar = false;
    }
}
