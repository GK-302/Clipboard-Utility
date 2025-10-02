// NotificationsService.cs
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.ViewModels;
using ClipboardUtility.src.Views;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;

namespace ClipboardUtility.Services
{
    public class NotificationsService
    {
        private readonly Lazy<NotificationWindow> _lazyWindow;
        private readonly NotificationViewModel _viewModel;
        private AppSettings _appSettings;

        // 連続した通知リクエストを制御するためのキャンセル機構
        private CancellationTokenSource _cts;

        public NotificationsService()
        {
            _viewModel = new NotificationViewModel();
            _lazyWindow = new Lazy<NotificationWindow>(InitializeWindow);

            try
            {
                // 初期設定は中央サービスから取得する（ファイル直読みはやめる）
                _appSettings = SettingsService.Instance.Current;
                Debug.WriteLine($"NotificationsService: 初期設定読み込み Offsets=({_appSettings.NotificationOffsetX},{_appSettings.NotificationOffsetY}) MinW={_appSettings.NotificationMinWidth}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotificationsService: failed to read initial settings: {ex}");
                _appSettings = new AppSettings();
            }

            // 設定変更通知を購読してランタイム設定を更新（デバッグログも出す）
            try
            {
                SettingsService.Instance.SettingsChanged += OnSettingsChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotificationsService: failed to subscribe to SettingsChanged: {ex}");
            }
        }

        private void OnSettingsChanged(object? sender, AppSettings newSettings)
        {
            try
            {
                Debug.WriteLine($"NotificationsService: SettingsChanged イベント受信 Offsets=({newSettings.NotificationOffsetX},{newSettings.NotificationOffsetY}) MinW={newSettings.NotificationMinWidth} Time={DateTime.Now:O}");

                // UI スレッドで状態を適用
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _appSettings = newSettings ?? new AppSettings();

                        if (_lazyWindow.IsValueCreated)
                        {
                            var window = _lazyWindow.Value;
                            window.MinWidth = _appSettings.NotificationMinWidth;
                            window.MaxWidth = _appSettings.NotificationMaxWidth;
                            window.MinHeight = _appSettings.NotificationMinHeight;
                            window.MaxHeight = _appSettings.NotificationMaxHeight;

                            Debug.WriteLine("NotificationsService: NotificationWindow のサイズ設定を更新しました。");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Debug.WriteLine($"NotificationsService.OnSettingsChanged: UI-apply failed: {innerEx}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotificationsService.OnSettingsChanged: failed: {ex}");
            }
        }

        /// <summary>
        /// ウィンドウの初期化処理
        /// </summary>
        /// <returns>初期化されたNotificationWindow</returns>
        private NotificationWindow InitializeWindow()
        {
            try
            {
                // UIスレッドでウィンドウを生成・初期化する
                return System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var window = new NotificationWindow
                        {
                            DataContext = _viewModel
                        };

                        // Apply sizing from settings
                        window.MinWidth = _appSettings.NotificationMinWidth;
                        window.MaxWidth = _appSettings.NotificationMaxWidth;
                        window.MinHeight = _appSettings.NotificationMinHeight;
                        window.MaxHeight = _appSettings.NotificationMaxHeight;

                        // Ensure the window sizes to content
                        window.SizeToContent = SizeToContent.WidthAndHeight;

                        // Force an initial layout pass so ActualWidth/ActualHeight become available
                        try
                        {
                            window.Show();
                            window.Hide();
                            window.UpdateLayout();
                        }
                        catch (Exception layoutEx)
                        {
                            Debug.WriteLine($"NotificationsService.InitializeWindow: layout pass failed: {layoutEx}");
                            // 続行可能なら無視して戻す
                        }

                        return window;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"NotificationsService.InitializeWindow: failed to create window: {ex}");
                        // フォールバック：最小限の NotificationWindow を返す（例外で失敗する可能性は低い）
                        var fallback = new NotificationWindow { DataContext = _viewModel };
                        return fallback;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotificationsService.InitializeWindow: dispatcher invoke failed: {ex}");
                // 最後の手段：UI スレッドでない場合は例外を再スローしないで新しいインスタンスを返す（呼び出し側で追加の保護がある想定）
                var fallbackWindow = new NotificationWindow { DataContext = _viewModel };
                return fallbackWindow;
            }
        }

        public async Task ShowNotification(string message, NotificationType type = NotificationType.Information)
        {
            Debug.WriteLine($"NotificationsService.ShowNotification: called with message='{message}', type={type}, Time={DateTime.Now:O}");

            try
            {
                // フラグに合わせて抑止する
                if (type == NotificationType.Copy && !_appSettings.ShowCopyNotification)
                {
                    Debug.WriteLine($"NotificationsService: suppressed COPY notification (message='{message}')");
                    return;
                }

                if (type == NotificationType.Operation && !_appSettings.ShowOperationNotification)
                {
                    Debug.WriteLine($"NotificationsService: suppressed OPERATION notification (message='{message}')");
                    return;
                }

                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                try
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var window = _lazyWindow.Value;
                            _viewModel.NotificationMessage = message;
                            
                            // confirm offset are loaded 
                            Debug.WriteLine($"NotificationsService.ShowNotification: Offsets=({_appSettings.NotificationOffsetX},{_appSettings.NotificationOffsetY}) MinW={_appSettings.NotificationMinWidth} Time={DateTime.Now:O}");

                            // ウィンドウの位置を計算
                            System.Windows.Point position;
                            try
                            {
                                position = MouseHelper.GetClampedPosition(window, _appSettings.NotificationOffsetX, _appSettings.NotificationOffsetY);
                            }
                            catch (Exception posEx)
                            {
                                Debug.WriteLine($"NotificationsService.ShowNotification: positioning failed: {posEx}");
                                position = new System.Windows.Point(
                                    SystemParameters.WorkArea.Width - window.Width - 16,
                                    SystemParameters.WorkArea.Height - window.Height - 16
                                );
                            }

                            window.Left = position.X;
                            window.Top = position.Y;

                            // 画面ピクセル色を検出してテキスト色を自動調整
                            try
                            {
                                Debug.WriteLine($"NotificationsService: Auto-adjusting colors for screen pixels at position ({position.X}, {position.Y})");
                                _viewModel.AutoAdjustColorsForScreenPosition(position.X, position.Y, window.ActualWidth, window.ActualHeight);
                            }
                            catch (Exception colorEx)
                            {
                                Debug.WriteLine($"NotificationsService: Color adjustment failed: {colorEx.Message}");
                                // フォールバック: デフォルト色を使用
                                _viewModel.SetColorsForScreenBackground(ColorHelper.GetDefaultBackgroundColor());
                            }

                            window.Visibility = Visibility.Visible;
                            Debug.WriteLine($"NotificationsService: Notification window shown at ({position.X}, {position.Y})");
                        }
                        catch (Exception uiEx)
                        {
                            Debug.WriteLine($"NotificationsService.ShowNotification: UI update failed: {uiEx}");
                        }
                    });

                    await Task.Delay(1500, token);

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (!token.IsCancellationRequested)
                            {
                                _lazyWindow.Value.Visibility = Visibility.Hidden;
                                Debug.WriteLine($"NotificationsService: Notification window hidden");
                            }
                        }
                        catch (Exception hideEx)
                        {
                            Debug.WriteLine($"NotificationsService.ShowNotification: hide failed: {hideEx}");
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("NotificationsService: Notification display was cancelled");
                    // キャンセル時は何もしない
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotificationsService.ShowNotification: unexpected exception: {ex}");
            }
        }
    }
}