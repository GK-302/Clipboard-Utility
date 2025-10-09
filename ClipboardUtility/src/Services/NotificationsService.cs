// NotificationsService.cs
using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using ClipboardUtility.src.Views;
using System.Diagnostics;
using System.Windows;

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
                // 表示抑止フラグ
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

                // 既存の表示をキャンセル
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                try
                {
                    double posX = 0, posY = 0, winW = 0, winH = 0;

                    // 1) UI スレッドでウィンドウを初期化して即時表示（色はデフォルト）
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var window = _lazyWindow.Value;
                            _viewModel.NotificationMessage = message;

                            Debug.WriteLine($"NotificationsService.ShowNotification: Offsets=({_appSettings.NotificationOffsetX},{_appSettings.NotificationOffsetY}) MinW={_appSettings.NotificationMinWidth} Time={DateTime.Now:O}");

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

                            // 一旦デフォルト背景色で表示（ブロッキングを避ける)
                            _viewModel.SetColorsForScreenBackground(ColorHelper.GetDefaultBackgroundColor());

                            window.Visibility = Visibility.Visible;
                            window.UpdateLayout();

                            posX = window.Left;
                            posY = window.Top;
                            winW = window.ActualWidth;
                            winH = window.ActualHeight;

                            Debug.WriteLine($"NotificationsService: Notification window shown at ({posX}, {posY}) size {winW}x{winH}");
                        }
                        catch (Exception uiEx)
                        {
                            Debug.WriteLine($"NotificationsService.ShowNotification: UI init failed: {uiEx}");
                        }
                    });

                    // 2) 色検出をバックグラウンドで非同期実行（UIをブロックしない）
                    System.Windows.Media.Color detectedColor = ColorHelper.GetDefaultBackgroundColor();
                    try
                    {
                        Debug.WriteLine("NotificationsService: Starting background color detection");
                        detectedColor = await Task.Run(() =>
                        {
                            token.ThrowIfCancellationRequested();
                            return ColorHelper.GetAverageBackgroundColor(posX, posY, winW, winH);
                        }, token);
                        Debug.WriteLine("NotificationsService: Background color detection completed");
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("NotificationsService: Color detection was cancelled");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"NotificationsService: Color detection failed: {ex.Message}");
                        FileLogger.LogException(ex, "NotificationsService: Color detection failed");
                        detectedColor = ColorHelper.GetDefaultBackgroundColor();
                    }

                    // 3) 検出色を UI スレッドで適用
                    try
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                if (!token.IsCancellationRequested)
                                {
                                    _viewModel.SetColorsForScreenBackground(detectedColor);
                                    Debug.WriteLine("NotificationsService: Applied detected colors to ViewModel");
                                }
                            }
                            catch (Exception applyEx)
                            {
                                Debug.WriteLine($"NotificationsService: Apply color failed: {applyEx.Message}");
                                FileLogger.LogException(applyEx, "NotificationsService: Apply color failed");
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("NotificationsService: Applying detected color cancelled");
                    }

                    // 表示保持時間（キャンセル対応）
                    await Task.Delay(1500, token);

                    // 非表示
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (!token.IsCancellationRequested)
                            {
                                _lazyWindow.Value.Visibility = Visibility.Hidden;
                                Debug.WriteLine("NotificationsService: Notification window hidden");
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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotificationsService.ShowNotification: unexpected exception: {ex}");
                FileLogger.LogException(ex, "NotificationsService.ShowNotification: unexpected");
            }
        }
    }
}