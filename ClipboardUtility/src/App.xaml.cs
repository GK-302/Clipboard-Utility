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
        private const string MutexName = "ClipboardUtility_{6F1A9C2E-3A4B-4D5E-9F12-ABCDEF123456}";

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew = false;
            try
            {
                _instanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Failed to create/open mutex: {ex}");
            }

            if (!createdNew)
            {
                Debug.WriteLine("Another instance is already running. Exiting.");
                Shutdown();
                return;
            }

            // スタートアップ登録はユーザーの設定画面 / ウェルカム画面で行うようにしたため
            // ここで自動的に登録は行わない。

            base.OnStartup(e);

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

            var tray = TaskTrayService.Instance;
            tray.Initialize();

            try
            {
                _welcomeService = new WelcomeService();
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

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mainViewModel?.Cleanup();
            TaskTrayService.Instance?.Dispose();
            try
            {
                _instanceMutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
            finally
            {
                _instanceMutex?.Dispose();
                _instanceMutex = null;
            }

            base.OnExit(e);
        }

        // Startup registration logic removed - handled by settings/welcome UI
    }
}