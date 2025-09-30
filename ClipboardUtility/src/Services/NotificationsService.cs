// NotificationsService.cs
using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.ViewModels;
using ClipboardUtility.src.Views;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboardUtility.Services
{
    public class NotificationsService
    {
        private readonly Lazy<NotificationWindow> _lazyWindow;
        private readonly NotificationViewModel _viewModel;

        // 連続した通知リクエストを制御するためのキャンセル機構
        private CancellationTokenSource _cts;

        public NotificationsService()
        {
            _viewModel = new NotificationViewModel();
            _lazyWindow = new Lazy<NotificationWindow>(InitializeWindow);
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
                // 初回は表示せずにロードだけを行う
                window.Show();
                window.Hide();
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

        /// <summary>
        /// 通知を表示します。連続して呼び出された場合、前の通知はキャンセルされ新しい通知が表示されます。
        /// </summary>
        public async Task ShowNotification(string message, NotificationType type = NotificationType.Information)
        {
            // 既存の待機処理（Task.Delay）があればキャンセルする
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                // UIスレッドでUI要素の更新と表示を行う
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Lazy<T>経由でウィンドウインスタンスを取得（初回ならここで初期化が走る）
                    var window = _lazyWindow.Value;

                    // ViewModelのプロパティを更新
                    _viewModel.NotificationMessage = message;

                    // ウィンドウの位置を調整
                    // System.Drawing.Point mousePosition = MouseHelper.GetCursorPosition();
                    window.Left = 0; // 位置調整はお好みで
                    window.Top = 0;

                    // ウィンドウを表示
                    window.Visibility = Visibility.Visible;
                });

                // 指定時間、表示を待機する
                await Task.Delay(1500, token);

                // UIスレッドでウィンドウを非表示にする
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 新しい通知によってキャンセルされていない場合（=tokenが生きてる場合）のみ非表示にする
                    if (!token.IsCancellationRequested)
                    {
                        _lazyWindow.Value.Visibility = Visibility.Hidden;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // 新しい通知リクエストによってキャンセルされた場合はここに来る。
                // 正常な動作なので、特に何もしない。
            }
        }
    }
}