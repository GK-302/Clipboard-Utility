using ClipboardUtility.src.ViewModels;
using System.Windows;

namespace ClipboardUtility.src.Views
{
    /// <summary>
    /// NotificationWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NotificationWindow : Window
    {
        public NotificationWindow()
        {
            InitializeComponent();
            this.DataContext = new NotificationViewModel();
        }
    }
}
