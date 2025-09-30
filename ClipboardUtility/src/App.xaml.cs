using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
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

            // Singletonインスタンスを取得
            var taskTrayService = TaskTrayService.Instance;

            _mainViewModel = new MainViewModel();
            _mainViewModel.SubscribeToTaskTrayEvents(taskTrayService);

            taskTrayService.Initialize();

            var mainWindow = new MainWindow(_mainViewModel, taskTrayService);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mainViewModel?.Cleanup();
            TaskTrayService.Instance?.Dispose(); // Singletonインスタンスを取得してDispose
            base.OnExit(e);
        }
    }

}
