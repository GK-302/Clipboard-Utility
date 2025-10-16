using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
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
        private Mutex? _instanceMutex;
        // 固有のミューテックス名（アプリケーションごとに変更してください）
        private const string MutexName = "ClipboardUtility_{6F1A9C2E-3A4B-4D5E-9F12-ABCDEF123456}";

        protected override void OnStartup(StartupEventArgs e)
        {
            // 二重起動防止のためミューテックスを作成
            bool createdNew = false;
            try
            {
                _instanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Failed to create/open mutex: {ex}");
                // アクセス権限の問題があっても続行は試みる
            }

            if (!createdNew)
            {
                Debug.WriteLine("Another instance is already running. Exiting.");
                // 既に起動しているため即座に終了
                Shutdown();
                return;
            }

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
            try
            {
                _instanceMutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // ミューテックスが現在所有されていない場合は無視
            }
            finally
            {
                _instanceMutex?.Dispose();
                _instanceMutex = null;
            }

            base.OnExit(e);
        }
    }

}
