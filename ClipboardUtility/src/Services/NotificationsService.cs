using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Views;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboardUtility.Services // namespaceをプロジェクトに合わせてください
{
    public class NotificationsService
    {
        private NotificationWindow _notificationWindow;
        private bool _isWindowInitialized = false;

        public async Task ShowNotification(string message)
        {
            // 初回呼び出し時にウィンドウを一度だけ生成する
            if (!_isWindowInitialized)
            {
                _notificationWindow = new NotificationWindow();
                // この時点では表示せず、裏で準備だけしておく
                _notificationWindow.Show();
                _notificationWindow.Hide();
                _isWindowInitialized = true;
            }

            // DataContextに表示したいメッセージを設定
            _notificationWindow.DataContext = message;

            // マウスの位置を取得してウィンドウを移動
            Point mousePosition = MouseHelper.GetCursorPosition();
            _notificationWindow.Left = mousePosition.X + 10; // 少し右にずらす
            _notificationWindow.Top = mousePosition.Y + 10;  // 少し下にずらす

            // ウィンドウを表示する
            _notificationWindow.Visibility = Visibility.Visible;

            // 1秒待つ (表示時間)
            // 0.5秒表示 + 0.5秒フェードアウトなら、合計1秒以上は必要
            await Task.Delay(1000);

            // ウィンドウを非表示にする（Closeではない！）
            // ※フェードアウトアニメーションを追加する場合は、アニメーション完了後にHide()を呼ぶ
            _notificationWindow.Visibility = Visibility.Hidden;
        }
    }
}
