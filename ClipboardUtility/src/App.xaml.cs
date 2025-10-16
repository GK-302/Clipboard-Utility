using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using System.Reflection;
using System.IO;

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

            // デバッグビルドではない場合にのみ、スタートアップ登録処理を実行します。
            // これにより、発行されたアプリケーションでのみこの処理が行われます。
#if !DEBUG
            RegisterInStartup();
#endif

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

        /// <summary>
        /// アプリケーションをWindowsのスタートアップに登録します。
        /// </summary>
        private void RegisterInStartup()
        {
            try
            {
                string productName = GetProductName();
                if (string.IsNullOrEmpty(productName)) return;

                string runKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                RegistryKey? startupKey = Registry.CurrentUser.OpenSubKey(runKeyPath, true);
                if (startupKey == null) return;

                if (startupKey.GetValue(productName) != null)
                {
                    startupKey.Close();
                    return;
                }

                string publisherName = GetPublisherName();
                string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string shortcutPath = Path.Combine(startupFolderPath, publisherName, productName + ".appref-ms");

                if (File.Exists(shortcutPath))
                {
                    startupKey.SetValue(productName, $"\"{shortcutPath}\"");
                }

                startupKey.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register startup application: {ex}");
            }
        }

        private string GetProductName()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return string.Empty;

            var productAttribute = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            return productAttribute?.Product ?? entryAssembly.GetName().Name ?? "ClipboardUtility";
        }

        private string GetPublisherName()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return string.Empty;

            var companyAttribute = entryAssembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            return companyAttribute?.Company ?? GetProductName();
        }
    }
}