using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ClipboardUtility.src.Services;

/// <summary>
/// Win32 APIを使用してクリップボードの変更を監視するサービス。
/// </summary>
public class ClipboardService : IDisposable
{
    // --- Win32 APIの定義 ---
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    // --- C# イベントの定義 ---
    /// <summary>
    /// クリップボードの内容が更新されたときに発生します。
    /// 更新されたテキストをstringとして渡します。
    /// </summary>
    public event EventHandler<string> ClipboardUpdated;

    private HwndSource? _hwndSource;
    private bool _isDisposed = false;

    /// <summary>
    /// 指定されたウィンドウハンドルを使用してクリップボードの監視を開始します。
    /// </summary>
    /// <param name="window">監視の親となるWPFウィンドウ</param>
    public void StartMonitoring(Window window)
    {
        if (_hwndSource != null) return;

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        if (_hwndSource == null)
        {
            throw new InvalidOperationException("Could not create HwndSource.");
        }

        // ウィンドウメッセージをフックするメソッドを追加
        _hwndSource.AddHook(WndProc);
        // クリップボード監視を開始
        AddClipboardFormatListener(windowHandle);
    }
    /// <summary>
    /// クリップボードにテキストを設定します。
    /// set clipboard text
    /// </summary>
    /// <param name="text"></param>
    public void SetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting clipboard text: {ex.Message}");
        }
    }

    /// <summary>
    /// ウィンドウメッセージを処理するフックメソッド。
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            try
            {
                // クリップボードにテキストデータが含まれているかチェック
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    // イベントを発行して、購読者に通知
                    ClipboardUpdated?.Invoke(this, text);
                }
            }
            catch (Exception ex)
            {
                // 他のアプリがクリップボードをロックしている場合など
                Console.WriteLine($"Error accessing clipboard: {ex.Message}");
            }
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// リソースを解放し、監視を停止します。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        if (_hwndSource != null)
        {
            RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}