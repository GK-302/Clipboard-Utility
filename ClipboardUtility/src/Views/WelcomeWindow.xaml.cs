using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;  // この行を追加
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MessageBox = System.Windows.MessageBox;
using System.ComponentModel;
using System.Globalization;

namespace ClipboardUtility.src.Views
{
    public partial class WelcomeWindow   : Window
    {
        private readonly WelcomeWindowViewModel _vm;  // この行を追加
        private string _initialCultureName;

        public WelcomeWindow()
        {
            InitializeComponent();
            // CultureProvider を作成して ViewModel に注入
            var cultureProvider = new CultureProvider();
            // ViewModelを設定（これらの行を追加）
            _vm = new WelcomeWindowViewModel(cultureProvider);
            DataContext = _vm;

            // ウィンドウ作成時のカルチャ名を保存（null 安全）
            _initialCultureName = _vm.SelectedCulture?.Name ?? CultureInfo.CurrentUICulture.Name;

            // 閉じるときに確認ダイアログを出すハンドラを登録
            this.Closing += WelcomeWindow_Closing;

            SourceInitialized += WelcomeWindow_SourceInitialized;
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }

        // 閉じる前の処理: 言語変更があれば確認ダイアログを出す
        private void WelcomeWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                var currentCulture = _vm.SelectedCulture?.Name ?? CultureInfo.CurrentUICulture.Name;
                if (!string.Equals(_initialCultureName, currentCulture, StringComparison.OrdinalIgnoreCase))
                {
                    var result = MessageBox.Show(
                        "言語を変更しました。アプリを再起動しますか？\n（再起動しないと一部表示が反映されない場合があります）",
                        "再起動の確認",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // DI コンテナ経由で再起動サービスを取得して再起動
                            IAppRestartService restartService;
                            try
                            {
                                restartService = App.Services.Get<IAppRestartService>();
                            }
                            catch
                            {
                                // フォールバック
                                restartService = new AppRestartService();
                            }

                            restartService.Restart();
                            // Restart() が Application.Shutdown() を行う想定なのでここでは閉じる許可
                        }
                        catch (Exception ex)
                        {
                            ClipboardUtility.src.Helpers.FileLogger.LogException(ex, "WelcomeWindow_Closing: Restart failed");
                            MessageBox.Show("アプリの再起動に失敗しました。手動で再起動してください。", "再起動失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                            // 再起動失敗でも閉じる動作は続行（必要なら Cancel にする）
                        }
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // 再起動しないを選択 => そのまま閉じる。初期値を更新して次回確認しないようにする。
                        _initialCultureName = currentCulture;
                    }
                    else // Cancel
                    {
                        // 閉じるを中止
                        e.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ClipboardUtility.src.Helpers.FileLogger.LogException(ex, "WelcomeWindow_Closing: error");
            }
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
