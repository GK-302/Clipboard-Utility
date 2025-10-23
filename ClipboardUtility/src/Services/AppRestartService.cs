using System;
using System.Diagnostics;
using System.Reflection; // ProcessPath のために必要ない場合がある
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace ClipboardUtility.src.Services;

internal sealed class AppRestartService : IAppRestartService
{
    public void Restart(string? args = null)
    {
        try
        {
            // 実行ファイルパスを取得 (最も信頼性が高い方法)
            // .NET Core/5+ では Environment.ProcessPath が .exe パスを正しく返す
            var exePath = Environment.ProcessPath;

            // .NET Framework で実行している場合などのフォールバック
            if (string.IsNullOrEmpty(exePath))
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName;
            }

            if (string.IsNullOrEmpty(exePath))
            {
                MessageBox.Show("アプリを再起動できませんでした。手動で再起動してください。", "再起動失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var psi = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Arguments = args ?? string.Empty,
            };

            Process.Start(psi);
        }
        catch (Exception ex) // ex をログに出力することを推奨
        {
            // ex.Message などをログに出力する
            MessageBox.Show("アプリを再起動できませんでした。手動で再起動してください。", "再起動失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // UI スレッドで安全に終了
        System.Windows.Application.Current?.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
    }
}