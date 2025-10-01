using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using ClipboardUtility.src.Views;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using ClipboardUtility.src.Helpers;
using System.Diagnostics;

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

            // 設定に保存されているカルチャを適用してからウィンドウを生成する
            try
            {
                var cultureName = SettingsService.Instance.Current?.CultureName;
                if (!string.IsNullOrEmpty(cultureName))
                {
                    var ci = new CultureInfo(cultureName);
                    CultureInfo.DefaultThreadCurrentCulture = ci;
                    CultureInfo.DefaultThreadCurrentUICulture = ci;
                    LocalizedStrings.Instance.ChangeCulture(ci);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply saved culture: {ex}");
            }

            // 既存の初期化処理を続ける...
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
