using ClipboardUtility.Services;
using ClipboardUtility.src.Coordinators;
using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using ClipboardUtility.src.Views;
using Microsoft.Extensions.DependencyInjection; // <--- DIパッケージをインポート
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace ClipboardUtility;

public partial class App : System.Windows.Application
{

    // <--- MainViewModelはOnExitでのみ使うため、フィールドとして保持
    private MainViewModel? _mainViewModel;

    private Mutex? _instanceMutex;
    private const string MutexName = "ClipboardUtility_{6F1A9C2E-3A4B-4D5E-9F12-ABCDEF123456}";

    public static IServiceProvider ServiceProvider { get; private set; }

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // --- シングルトンサービス (アプリ全体で1つ) ---

        // 修正: .Instance 呼び出しを、DIコンテナによるシングルトン管理に変更
        services.AddSingleton<SettingsService>();
        services.AddSingleton<TaskTrayService>();

        // インターフェース指定のシングルトン (変更なし)
        services.AddSingleton<ICultureProvider, CultureProvider>();
        services.AddSingleton<IAppRestartService, AppRestartService>();

        // <--- 追加: アプリのコアサービス (シングルトン)
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<TextProcessingService>();
        services.AddSingleton<PresetService>();

        // --- Transient (要求されるたびに新しいインスタンス) ---

        // <--- 追加: VMが依存するサービス
        services.AddTransient<NotificationsService>();
        services.AddTransient<ClipboardOperationService>();

        // ViewModel (MainViewModel以外も登録)
        services.AddTransient<MainViewModel>();
        services.AddTransient<WelcomeWindowViewModel>();
        services.AddTransient<ClipboardManagerViewModel>();
        // ※SettingsViewModelはSettingsWindowが内部でnewしているため、ここでは不要

        // View (Window)
        services.AddTransient<MainWindow>();
        services.AddTransient<WelcomeWindow>();
        services.AddTransient<ClipboardManagerWindow>();
        // ※SettingsWindowはMainViewModelが動的にnewするため、ここでは不要

        // その他
        services.AddTransient<WelcomeService>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Mutexによる二重起動防止
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

        base.OnStartup(e);


        try
        {
            var settings = ServiceProvider.GetRequiredService<SettingsService>();
            var cultureName = settings.Current?.CultureName;
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

        var tray = ServiceProvider.GetRequiredService<TaskTrayService>();
        tray.Initialize();

        try
        {
            var welcomeService = ServiceProvider.GetRequiredService<WelcomeService>();
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                welcomeService.ShowWelcomeIfAppropriate();
            }));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize WelcomeService: {ex}");
        }

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

        // OnExit で Cleanup を呼ぶために MainViewModel のインスタンスを保持
        _mainViewModel = mainWindow.DataContext as MainViewModel; // MainWindowがVMをDataContextに設定している前提

        this.MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Cleanup();

        var trayService = ServiceProvider.GetService<TaskTrayService>();
        trayService?.Dispose();

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
}