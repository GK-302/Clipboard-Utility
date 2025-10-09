using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ClipboardUtility.src.Services;

/// <summary>
/// Win32 APIを使用してクリップボードの変更を監視するサービス。
/// 最小限の機能に整理し、例外（特に COMException ）は呼び出し元に伝搬する設計にしています。
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

    /// <summary>
    /// クリップボードの内容が更新されたときに発生します。
    /// 更新されたテキストを string として渡します。
    /// </summary>
    public event EventHandler<string> ClipboardUpdated;

    private HwndSource? _hwndSource;
    private bool _isDisposed = false;
    private readonly object _lockObject = new();

    public void StartMonitoring(Window window)
    {
        if (_hwndSource != null) return;

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        if (_hwndSource == null)
        {
            throw new InvalidOperationException("Could not create HwndSource.");
        }

        _hwndSource.AddHook(WndProc);
        _ = AddClipboardFormatListener(windowHandle);
    }

    /// <summary>
     /// クリップボードにテキストを設定します（単純化版）。
    /// 失敗時は例外（COMExceptionなど）を再スローします。呼び出し側で try/catch してください。
    /// </summary>
    public async Task<bool> SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        const int postSetDelayMs = 50;

        Trace.WriteLine($"ClipboardService.SetTextAsync: setting text length={text?.Length ?? 0}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_lockObject)
            {
                System.Windows.Clipboard.SetText(text);
            }
            Trace.WriteLine("ClipboardService.SetTextAsync: SetText succeeded");

            // 設定後に短い遅延を入れて検証
            await Task.Delay(postSetDelayMs, cancellationToken);

            // 単発検証
            bool verified = await VerifyClipboardContentAsync(text, cancellationToken);
            Trace.WriteLine($"ClipboardService.SetTextAsync: verification result={verified}");
            return verified;
        }
        catch (COMException)
        {
            Trace.WriteLine("ClipboardService.SetTextAsync: COMException occurred while setting clipboard - rethrowing to caller");
            throw; // 呼び出し元に任せる
        }
        catch (TaskCanceledException tce)
        {
            Trace.WriteLine($"ClipboardService.SetTextAsync: Operation was canceled: {tce.Message}");
            Trace.WriteLine(tce.ToString());
            throw;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"ClipboardService.SetTextAsync: Unexpected exception: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// クリップボードの内容が期待されるテキストと一致するかを単発で検証します。
    /// 例外は呼び出し元へ伝搬します（COMException など）。
    /// </summary>
    public async Task<bool> VerifyClipboardContentAsync(string expectedText, CancellationToken cancellationToken = default)
    {
        const int verificationDelayMs = 25;

        Trace.WriteLine("ClipboardService.VerifyClipboardContentAsync: starting single verification");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string actualText;
            lock (_lockObject)
            {
                if (!System.Windows.Clipboard.ContainsText())
                {
                    Trace.WriteLine("ClipboardService.VerifyClipboardContentAsync: clipboard does not contain text");
                    return false;
                }
                actualText = System.Windows.Clipboard.GetText();
            }

            Trace.WriteLine($"ClipboardService.VerifyClipboardContentAsync: retrieved text length={actualText?.Length ?? 0}");

            bool equal = string.Equals(actualText, expectedText, StringComparison.Ordinal);
            // 一応少し待つ選択肢を残す（呼び出し側のキャンセル対応）
            await Task.Delay(verificationDelayMs, cancellationToken);
            return equal;
        }
        catch (COMException)
        {
            Trace.WriteLine("ClipboardService.VerifyClipboardContentAsync: COMException during verification - rethrowing");
            throw;
        }
        catch (TaskCanceledException)
        {
            Trace.WriteLine("ClipboardService.VerifyClipboardContentAsync: verification cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"ClipboardService.VerifyClipboardContentAsync: Unexpected exception: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// クリップボードにテキストを設定します（同期）。失敗時は例外を再スローします。
    /// </summary>
    public void SetText(string text)
    {
        Trace.WriteLine($"ClipboardService.SetText: setting text length={text?.Length ?? 0}");
        try
        {
            lock (_lockObject)
            {
                System.Windows.Clipboard.SetText(text);
            }
            Trace.WriteLine("ClipboardService.SetText: succeeded");
        }
        catch (COMException)
        {
            Trace.WriteLine("ClipboardService.SetText: COMException - rethrowing");
            throw;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"ClipboardService.SetText: Unexpected exception: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// クリップボードからテキストを取得します。COMException は呼び出し元へ伝搬します。
    /// </summary>
    public string GetTextSafely()
    {
        Trace.WriteLine("ClipboardService.GetTextSafely: attempting to read clipboard");

        // 最小限の処理：失敗は例外で伝搬
        lock (_lockObject)
        {
            if (!System.Windows.Clipboard.ContainsText())
            {
                Trace.WriteLine("ClipboardService.GetTextSafely: clipboard does not contain text");
                return null;
            }
            string text = System.Windows.Clipboard.GetText();
            Trace.WriteLine($"ClipboardService.GetTextSafely: retrieved text length={text?.Length ?? 0}");
            return text;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            Trace.WriteLine("ClipboardService: WM_CLIPBOARDUPDATE received");

            try
            {
                // WndProc 上では例外が発生してもアプリを壊さないように catch する
                string text = null;
                try
                {
                    text = GetTextSafely();
                }
                catch (COMException comEx)
                {
                    Trace.WriteLine($"ClipboardService.WndProc: COMException reading clipboard: {comEx.Message}");
                    // 必要ならここでイベントや通知を上げる（最低限はログ）
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"ClipboardService.WndProc: Unexpected error reading clipboard: {ex.Message}");
                }

                if (text != null)
                {
                    Trace.WriteLine($"ClipboardService.WndProc: raising ClipboardUpdated with length={text.Length}");
                    ClipboardUpdated?.Invoke(this, text);
                }
                else
                {
                    Trace.WriteLine("ClipboardService.WndProc: no text available after read attempt");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ClipboardService.WndProc: handler error: {ex.Message}");
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        if (_hwndSource != null)
        {
            try
            {
                _ = RemoveClipboardFormatListener(_hwndSource.Handle);
                _hwndSource.RemoveHook(WndProc);
                _hwndSource.Dispose();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ClipboardService.Dispose: error disposing: {ex.Message}");
            }
            finally
            {
                _hwndSource = null;
            }
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}