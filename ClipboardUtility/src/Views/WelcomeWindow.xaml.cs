using ClipboardUtility.src.Services;
using ClipboardUtility.src.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MessageBox = System.Windows.MessageBox;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls; // スライドナビゲーションに必要
using ClipboardUtility.src.Views.WelcomeSlides;
using CheckBox = System.Windows.Controls.CheckBox; // UserControlの参照に必要

namespace ClipboardUtility.src.Views
{
    public partial class WelcomeWindow : Window
    {
        // 依存関係とViewModel
        private readonly WelcomeWindowViewModel _vm;
        private readonly SettingsService _settingsService;
        private readonly IAppRestartService _restartService;
        private string _initialCultureName;

        // スライド制御用
        private int _totalSlides;

        public WelcomeWindow(
            WelcomeWindowViewModel viewModel,
            IAppRestartService restartService,
            SettingsService settingsService)
        {
            InitializeComponent();

            // 依存関係をフィールドに保存
            _vm = viewModel;
            _restartService = restartService;
            _settingsService = settingsService;

            // ViewModelをDataContextに設定
            DataContext = _vm;

            // 初期カルチャを保存 (言語変更の比較用)
            _initialCultureName = _vm.SelectedCulture?.Name ?? CultureInfo.CurrentUICulture.Name;

            // イベントハンドラを登録
            this.Closing += WelcomeWindow_Closing;
            SourceInitialized += WelcomeWindow_SourceInitialized;

            // スライドの総数を取得
            _totalSlides = SlideTabControl.Items.Count;
            UpdateNavigationUI();
        }

        // --- 1. スライドナビゲーション (前回提案のロジック) ---

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (SlideTabControl.SelectedIndex > 0)
            {
                SlideTabControl.SelectedIndex--;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (SlideTabControl.SelectedIndex < _totalSlides - 1)
            {
                // 次のスライドへ
                SlideTabControl.SelectedIndex++;
            }
            else
            {
                // 最後のスライドで「完了 (Done)」が押された
                // 設定を保存してウィンドウを閉じる
                SaveSettingsAndClose();
            }
        }

        private void SlideTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != SlideTabControl)
                return;
            UpdateNavigationUI();
        }

        private void UpdateNavigationUI()
        {
            int currentIndex = SlideTabControl.SelectedIndex;

            // ページ番号
            PageIndicator.Text = $"{currentIndex + 1} / {_totalSlides}";

            // 「前へ」ボタンの有効/無効
            BackButton.IsEnabled = currentIndex > 0;

            // 「次へ」/「完了」ボタンのテキスト切り替え
            if (currentIndex == _totalSlides - 1)
            {
                // TODO: "Done" も多言語化 (LocalizedStrings.Instance にキーを追加)
                NextButton.Content = "Done";
            }
            else
            {
                // TODO: "Next" も多言語化 (LocalizedStrings.Instance にキーを追加)
                NextButton.Content = "Next";
            }
        }

        // --- 2. 設定保存 (ユーザー提供のButton_Clickロジックをベースに修正) ---

        private void SaveSettingsAndClose()
        {
            bool neverShowAgain = NeverShowAgainCheckBox.IsChecked == true;

            try
            {
                // 設定を保存
                var settings = _settingsService.Current ?? new ClipboardUtility.src.Models.AppSettings();
                settings.ShowWelcomeNotification = !neverShowAgain;
                _settingsService.Save(settings);
            }
            catch (Exception ex)
            {
                ClipboardUtility.src.Helpers.FileLogger.LogException(ex, "WelcomeWindow.SaveSettingsAndClose: Save settings failed");
                MessageBox.Show("設定の保存に失敗しました。");
            }
            finally
            {
                // ウィンドウを閉じる (これにより WelcomeWindow_Closing がトリガーされる)
                this.Close();
            }
        }

        // --- 3. 再起動確認 (ユーザー提供のロジック) ---

        private void WelcomeWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                // 言語設定が変更されたかチェック
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
                            // アプリを再起動
                            _restartService.Restart();
                        }
                        catch (Exception ex)
                        {
                            ClipboardUtility.src.Helpers.FileLogger.LogException(ex, "WelcomeWindow_Closing: Restart failed");
                            MessageBox.Show("アプリの再起動に失敗しました。手動で再起動してください。", "再起動失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // 再起動しない場合は、現在のカルチャを「初期値」として更新
                        _initialCultureName = currentCulture;
                    }
                    else // Cancel
                    {
                        // ウィンドウを閉じるのをキャンセル
                        e.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ClipboardUtility.src.Helpers.FileLogger.LogException(ex, "WelcomeWindow_Closing: error");
            }
        }

        // --- 4. タイトルバー制御 (ユーザー提供のロジック) ---

        private void WelcomeWindow_SourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                RemoveTitleBarButtons();
            }
            catch (Exception ex)
            {
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
            // const uint WS_SYSMENU = 0x00080000; // 「X」ボタンを含むシステムメニュー

            IntPtr stylePtr = GetWindowLongPtr(hWnd, GWL_STYLE);
            ulong style = (ulong)stylePtr.ToInt64();

            // XAMLで ResizeMode="NoResize" を設定しているため、
            // 最小化・最大化ボタンのみを（念のため）無効化します。
            // WS_SYSMENU を削除すると「X」ボタンも消えてしまうため、ここでは残します。
            ulong newStyle = style & ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX);

            SetWindowLongPtr(hWnd, GWL_STYLE, new IntPtr((long)newStyle));

            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOZORDER = 0x0004;
            const uint SWP_FRAMECHANGED = 0x0020;
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        // --- 5. Win32 API定義 (ユーザー提供のロジック) ---

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newValue)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newValue);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, newValue.ToInt32()));
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
    }
}