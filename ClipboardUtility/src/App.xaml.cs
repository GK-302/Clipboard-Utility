using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using System.Diagnostics;
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
        private WelcomeService _welcomeService;

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
            // WelcomeService を生成・表示（UI スレッドで実行）
            try
            {
                _welcomeService = new WelcomeService();
                // 非同期に表示したければ BeginInvoke を使う
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _welcomeService.ShowWelcomeIfAppropriate();
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize WelcomeService: {ex}");
            }
            _mainViewModel = new MainViewModel();
            _mainViewModel.SubscribeToTaskTrayEvents(TaskTrayService.Instance);

            var mainWindow = new MainWindow(_mainViewModel, TaskTrayService.Instance);
            this.MainWindow = mainWindow;

            // テスト用に一時的に表示（完成したら Show() を削除して隠す）
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
