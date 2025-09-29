using ClipboardUtility.src.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClipboardUtility;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // ViewModelのインスタンスを作成
        _viewModel = new MainViewModel();
        // このウィンドウのDataContextにViewModelを設定
        // これにより、XAMLの {Binding} がViewModelのプロパティと繋がる
        this.DataContext = _viewModel;

        // Windowがロードされた後にViewModelの初期化処理を呼ぶ
        this.Loaded += MainWindow_Loaded;
        // Windowが閉じられるときにクリーンアップ処理を呼ぶ
        this.Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // このウィンドウ自体を渡して、クリップボード監視を開始させる
        _viewModel.Initialize(this);
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Cleanup();
    }
}