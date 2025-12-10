using ClipboardUtility.src.ViewModels;
using System.Windows;

namespace ClipboardUtility.src.Views;

/// <summary>
/// NotificationWindow.xaml の相互作用ロジック
/// </summary>
public partial class NotificationWindow : Window
{
    // <--- 変更: コンストラクタで ViewModel を受け取る
    public NotificationWindow(NotificationViewModel viewModel)
    {
        InitializeComponent();

        // <--- 変更: 'new' をやめ、注入された viewModel を設定する
        this.DataContext = viewModel;
    }
}