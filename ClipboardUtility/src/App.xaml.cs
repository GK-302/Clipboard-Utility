using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using ClipboardUtility.src.Views;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;

namespace ClipboardUtility
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private TaskTrayService _taskTrayService;
        private MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var tray = TaskTrayService.Instance;
            tray.Initialize();

            _mainViewModel = new MainViewModel();
            _mainViewModel.SubscribeToTaskTrayEvents(TaskTrayService.Instance);

            var mainWindow = new MainWindow(_mainViewModel, TaskTrayService.Instance);
            this.MainWindow = mainWindow;
            mainWindow.Show(); // トレイアプリとしてメインウィンドウを隠したいなら Show を省いて Hide する等
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mainViewModel?.Cleanup();
            TaskTrayService.Instance?.Dispose(); // Singletonインスタンスを取得してDispose
            base.OnExit(e);
        }
    }

}
