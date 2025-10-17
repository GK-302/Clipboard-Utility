using ClipboardUtility.src.ViewModels;  // この行を追加
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipboardUtility.src.Views
{
    public partial class WelcomeWindow : Window
    {
        private readonly WelcomeWindowViewModel _vm;  // この行を追加

        public WelcomeWindow()
        {
            InitializeComponent();
            
            // ViewModelを設定（これらの行を追加）
            _vm = new WelcomeWindowViewModel();
            DataContext = _vm;

            SourceInitialized += WelcomeWindow_SourceInitialized;
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }

        private void WelcomeWindow_SourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                RemoveTitleBarButtons();
            }
            catch (Exception ex)
            {
                // 失敗しても例外が UI を壊さないようにログだけ残す
                ClipboardUtility.src.Helpers.FileLogger.LogException(ex, "WelcomeWindow: RemoveTitleBarButtons failed");
            }
        }

        private void RemoveTitleBarButtons()
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hWnd = helper.Handle;
            if (hWnd == IntPtr.Zero) return;

            const int GWL_STYLE = -16;
            const uint WS_MINIMIZEBOX = 0x00020000;
            const uint WS_MAXIMIZEBOX = 0x00010000;
            const uint WS_SYSMENU = 0x00080000;

            // 現在のスタイルを取得
            IntPtr stylePtr = GetWindowLongPtr(hWnd, GWL_STYLE);
            ulong style = (ulong)stylePtr.ToInt64();

            // 最寄りのビットをクリアしてボタンを非表示にする
            // ここでは Minimize / Maximize / System menu (Close含む) を削除してタイトルバーのボタンを全て取り除く
            ulong newStyle = style & ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);

            // 設定を反映
            SetWindowLongPtr(hWnd, GWL_STYLE, new IntPtr((long)newStyle));

            // 非クライアント領域を更新して変更を反映させる
            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOZORDER = 0x0004;
            const uint SWP_FRAMECHANGED = 0x0020;
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        // 32/64-bit 対応の Get/Set wrappers
        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr64(hWnd, nIndex);
            }
            else
            {
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
            }
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newValue)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hWnd, nIndex, newValue);
            }
            else
            {
                return new IntPtr(SetWindowLong32(hWnd, nIndex, newValue.ToInt32()));
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool neverShowAgain = IsShowAgain.IsChecked == true;
            try
            {
                // 現在の設定を取得（null安全）
                var settings = ClipboardUtility.src.Services.SettingsService.Instance.Current ?? new ClipboardUtility.src.Models.AppSettings();

                // プロパティを更新
                settings.ShowWelcomeNotification = !neverShowAgain;

                // 保存（SettingsService がファイル書き込みと通知を行う）
                ClipboardUtility.src.Services.SettingsService.Instance.Save(settings);

            }
            catch (Exception ex)
            {
                // エラーはログに残して UI に通知
                ClipboardUtility.src.Helpers.FileLogger.LogException(ex, "WelcomeWindow.Button_Click: Save settings failed");
                System.Windows.MessageBox.Show("設定の保存に失敗しました。");
            }
            finally {
                // ウィンドウを閉じる
                this.Close();
            }
        }
    }
}
