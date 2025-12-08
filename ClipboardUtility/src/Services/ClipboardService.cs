using ClipboardUtility.src.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

    /// <summary>
    /// クリップボード操作で発生したエラーを外部に通知するイベント
    /// </summary>
    public event EventHandler<ClipboardErrorEventArgs>? ClipboardError;

    public sealed class ClipboardErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Context { get; }

        public ClipboardErrorEventArgs(Exception exception, string context)
        {
            Exception = exception;
            Context = context;
        }
    }

    private HwndSource? _hwndSource;
    private bool _isDisposed = false;
    private readonly object _lockObject = new();

    // 最後の読み取り試行回数（通知用のデバッグ表示に使用）
    public int LastReadAttempts { get; private set; } = 0;

    public void StartMonitoring(Window window)
    {
        FileLogger.Log("ClipboardService: StartMonitoring called");
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

        Debug.WriteLine($"ClipboardService.SetTextAsync: setting text length={text?.Length ?? 0}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_lockObject)
            {
                System.Windows.Clipboard.SetText(text);
            }
            Debug.WriteLine("ClipboardService.SetTextAsync: SetText succeeded");

            // 設定後に短い遅延を入れて検証
            await Task.Delay(postSetDelayMs, cancellationToken);

            // 単発検証
            bool verified = await VerifyClipboardContentAsync(text, cancellationToken);
            Debug.WriteLine($"ClipboardService.SetTextAsync: verification result={verified}");
            return verified;
        }
        catch (COMException ex)
        {
            Debug.WriteLine("ClipboardService.SetTextAsync: COMException occurred while setting clipboard - rethrowing to caller");
            FileLogger.LogException(ex, "ClipboardService.SetTextAsync: COMException");
            throw; // 呼び出し元に任せる
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine("ClipboardService.SetTextAsync: Operation was canceled");
            FileLogger.LogException(ex, "ClipboardService.SetTextAsync: TaskCanceled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardService.SetTextAsync: Unexpected exception: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardService.SetTextAsync: Unexpected");
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

        Debug.WriteLine("ClipboardService.VerifyClipboardContentAsync: starting single verification");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string actualText;
            lock (_lockObject)
            {
                if (!System.Windows.Clipboard.ContainsText())
                {
                    Debug.WriteLine("ClipboardService.VerifyClipboardContentAsync: clipboard does not contain text");
                    return false;
                }
                actualText = System.Windows.Clipboard.GetText();
            }

            Debug.WriteLine($"ClipboardService.VerifyClipboardContentAsync: retrieved text length={actualText?.Length ?? 0}");

            bool equal = string.Equals(actualText, expectedText, StringComparison.Ordinal);
            // 一応少し待つ選択肢を残す（呼び出し側のキャンセル対応）
            await Task.Delay(verificationDelayMs, cancellationToken);
            return equal;
        }
        catch (COMException ex)
        {
            Debug.WriteLine("ClipboardService.VerifyClipboardContentAsync: COMException during verification - rethrowing");
            FileLogger.LogException(ex, "ClipboardService.VerifyClipboardContentAsync: COMException");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine("ClipboardService.VerifyClipboardContentAsync: verification cancelled");
            FileLogger.LogException(ex, "ClipboardService.VerifyClipboardContentAsync: TaskCanceled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardService.VerifyClipboardContentAsync: Unexpected exception: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardService.VerifyClipboardContentAsync: Unexpected");
            throw;
        }
    }

    /// <summary>
    /// クリップボードにテキストを設定します（同期）。失敗時は例外を再スローします。
    /// </summary>
    public void SetText(string text)
    {
        Debug.WriteLine($"ClipboardService.SetText: setting text length={text?.Length ?? 0}");
        try
        {
            lock (_lockObject)
            {
                System.Windows.Clipboard.SetText(text);
            }
            Debug.WriteLine("ClipboardService.SetText: succeeded");
        }
        catch (COMException ex)
        {
            Debug.WriteLine("ClipboardService.SetText: COMException - rethrowing");
            FileLogger.LogException(ex, "ClipboardService.SetText: COMException");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClipboardService.SetText: Unexpected exception: {ex.Message}");
            FileLogger.LogException(ex, "ClipboardService.SetText: Unexpected");
            throw;
        }
    }

    /// <summary>
    /// クリップボードからテキストを取得します。COMException は呼び出し元へ伝搬します。
    /// </summary>
    public string GetTextSafely()
    {
        Debug.WriteLine("ClipboardService.GetTextSafely: attempting to read clipboard");

        // 最小限の処理：失敗は例外で伝搬
        lock (_lockObject)
        {
            if (!System.Windows.Clipboard.ContainsText())
            {
                Debug.WriteLine("ClipboardService.GetTextSafely: clipboard does not contain text");
                return null;
            }
            string text = System.Windows.Clipboard.GetText();
            Debug.WriteLine($"ClipboardService.GetTextSafely: retrieved text length={text?.Length ?? 0}");
            return text;
        }
    }

    private void OnClipboardError(Exception ex, string context)
    {
        try
        {
            ClipboardError?.Invoke(this, new ClipboardErrorEventArgs(ex, context));
        }
        catch (Exception evEx)
        {
            // イベント購読側で例外が出てもログに残すだけにする
            FileLogger.LogException(evEx, "ClipboardService.OnClipboardError");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            Debug.WriteLine("ClipboardService: WM_CLIPBOARDUPDATE received");

            try
            {
                // WndProc 上では例外が発生してもアプリを壊さないように catch する
                string text = null;
                LastReadAttempts = 0;
                const int maxAttempts = 3;
                const int delayMs = 25;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        text = GetTextSafely();
                        LastReadAttempts = attempt;
                        if (text != null)
                        {
                            break;
                        }
                    }
                    catch (COMException comEx)
                    {
                        LastReadAttempts = attempt;
                        Debug.WriteLine($"ClipboardService.WndProc: COMException reading clipboard (attempt {attempt}): {comEx.Message}");
                        FileLogger.LogException(comEx, "ClipboardService.WndProc: COMException");

                        if (attempt == maxAttempts)
                        {
                            // 外部へ通知（UI スレッド上で呼ばれる点に注意）
                            OnClipboardError(comEx, "WndProc:GetTextSafely");
                        }
                    }
                    catch (Exception ex)
                    {
                        LastReadAttempts = attempt;
                        Debug.WriteLine($"ClipboardService.WndProc: Unexpected error reading clipboard (attempt {attempt}): {ex.Message}");
                        FileLogger.LogException(ex, "ClipboardService.WndProc: Unexpected");
                        if (attempt == maxAttempts)
                        {
                            OnClipboardError(ex, "WndProc:GetTextSafely");
                        }
                    }

                    // 次の試行まで少し待機
                    System.Threading.Thread.Sleep(delayMs);
                }

                if (text != null)
                {
                    Debug.WriteLine($"ClipboardService.WndProc: raising ClipboardUpdated with length={text.Length}");
                    ClipboardUpdated?.Invoke(this, text);
                }
                else
                {
                    Debug.WriteLine("ClipboardService.WndProc: no text available after read attempt");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClipboardService.WndProc: handler error: {ex.Message}");
                FileLogger.LogException(ex, "ClipboardService.WndProc: HandlerError");
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
                Debug.WriteLine($"ClipboardService.Dispose: error disposing: {ex.Message}");
                FileLogger.LogException(ex, "ClipboardService.Dispose: Error");
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