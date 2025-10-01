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

            // 初期設定は中央サービスから取得する（ファイル直読みはやめる）
            _appSettings = SettingsService.Instance.Current;
            Debug.WriteLine($"NotificationsService: 初期設定読み込み Offsets=({_appSettings.NotificationOffsetX},{_appSettings.NotificationOffsetY}) MinW={_appSettings.NotificationMinWidth}");

            // 設定変更通知を購読してランタイム設定を更新（デバッグログも出す）
            SettingsService.Instance.SettingsChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, AppSettings newSettings)
        {
            Debug.WriteLine($"NotificationsService: SettingsChanged イベント受信 Offsets=({newSettings.NotificationOffsetX},{newSettings.NotificationOffsetY}) MinW={newSettings.NotificationMinWidth} Time={DateTime.Now:O}");

            // UI スレッドで状態を適用
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _appSettings = newSettings;

                if (_lazyWindow.IsValueCreated)
                {
                    var window = _lazyWindow.Value;
                    window.MinWidth = _appSettings.NotificationMinWidth;
                    window.MaxWidth = _appSettings.NotificationMaxWidth;
                    window.MinHeight = _appSettings.NotificationMinHeight;
                    window.MaxHeight = _appSettings.NotificationMaxHeight;

                    Debug.WriteLine("NotificationsService: NotificationWindow のサイズ設定を更新しました。");
                }
            });
        }

        /// <summary>
        /// ウィンドウの初期化処理
        /// </summary>
        /// <returns>初期化されたNotificationWindow</returns>
        private NotificationWindow InitializeWindow()
        {
            // UIスレッドでウィンドウを生成・初期化する
            return System.Windows.Application.Current.Dispatcher.Invoke(() =>
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
                window.Show();
                window.Hide();
                window.UpdateLayout();

                return window;
            });
        }

        public async Task ShowsimultaneousNotification(string operationMessage, NotificationType type = NotificationType.Information)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var window = _lazyWindow.Value;
                    _viewModel.NotificationsimultaneousMessage = operationMessage;

                    window.Left = 0;
                    window.Top = 0;
                    window.Visibility = Visibility.Visible;
                });

                await Task.Delay(1500, token);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        _lazyWindow.Value.Visibility = Visibility.Hidden;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は何もしない
            }
        }

        public async Task ShowNotification(string message, NotificationType type = NotificationType.Information)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var window = _lazyWindow.Value;
                    _viewModel.NotificationMessage = message;
                    // confirm offset are loaded 
                    Debug.WriteLine($"NotificationsService.ShowNotification: Offsets=({_appSettings.NotificationOffsetX},{_appSettings.NotificationOffsetY}) MinW={_appSettings.NotificationMinWidth} Time={DateTime.Now:O}");

                    try
                    {
                        var pos = MouseHelper.GetClampedPosition(window, _appSettings.NotificationOffsetX, _appSettings.NotificationOffsetY);
                        window.Left = pos.X;
                        window.Top = pos.Y;
                    }
                    catch
                    {
                        window.Left = SystemParameters.WorkArea.Width - window.Width - 16;
                        window.Top = SystemParameters.WorkArea.Height - window.Height - 16;
                    }

                    window.Visibility = Visibility.Visible;
                });

                await Task.Delay(1500, token);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        _lazyWindow.Value.Visibility = Visibility.Hidden;
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}