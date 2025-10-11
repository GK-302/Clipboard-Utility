using System;
using System.Windows;
using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Views;

namespace ClipboardUtility.src.Services;

internal class WelcomeService
{
    private AppSettings _settings;
    public WelcomeService()
    {
        // ランタイム設定を取得（SettingsService を使用）
        _settings = SettingsService.Instance.Current ?? new AppSettings();
        // 設定変更を監視して _settings を更新
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
    }
    private void OnSettingsChanged(object? sender, AppSettings newSettings)
    {
        // SettingsService.NotifySettingsChanged は UI スレッド経由で呼ばれるため
        // ここは通常 UI スレッド上で実行されますが、安全のため Dispatcher を考慮しても良いです。
        _settings = newSettings ?? new AppSettings();
        FileLogger.Log($"WelcomeService: Settings updated. ShowOperationNotification={_settings.ShowOperationNotification}, NotificationDelay={_settings.NotificationDelay}");
    }
    /// <summary>
    /// 起動時にウェルカムを表示する（例）。UI 呼び出しは Dispatcher 経由で行う。
    /// 設定に基づいた表示制御はここで行う。
    /// </summary>
    public void ShowWelcomeIfAppropriate()
    {
        try
        {
            if (!_settings.ShowWelcomeNotification)
            {
                FileLogger.Log("WelcomeService: ShowOperationNotification disabled -> skipping welcome.");
                return;
            }

            // UI スレッドでウィンドウを表示
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    var win = new WelcomeWindow();
                    // 必要なら設定の値を渡して初期化する
                    // e.g. win.DataContext = new WelcomeViewModel(_settings);
                    win.Show();
                }
                catch (Exception ex)
                {
                    FileLogger.LogException(ex, "WelcomeService.ShowWelcomeIfAppropriate: UI show failed");
                }
            });
        }
        catch (Exception ex)
        {
            FileLogger.LogException(ex, "WelcomeService.ShowWelcomeIfAppropriate: failed");
        }
    }
}
