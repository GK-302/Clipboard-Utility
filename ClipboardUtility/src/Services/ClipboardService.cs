using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Threading;
using System.Diagnostics;

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
    private readonly object _lockObject = new object();

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
    /// クリップボードにテキストを設定します。再試行と検証機能付き。
    /// 
    /// 処理の流れ：
    /// 1. 最大3回まで再試行を行う
    /// 2. 各試行で System.Windows.Clipboard.SetText() を実行
    /// 3. 設定後に VerifyClipboardContentAsync() で検証
    /// 4. 失敗時は指数バックオフで遅延してから再試行
    /// 5. COMException や TaskCanceledException を適切に処理
    /// </summary>
    /// <param name="text">設定するテキスト</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>設定が成功したかどうか</returns>
    public async Task<bool> SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;      // 最大再試行回数
        const int delayMs = 50;        // 基本遅延時間（ミリ秒）

        Debug.WriteLine($"Starting clipboard text setting operation. Text length: {text?.Length ?? 0}");

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Debug.WriteLine($"Clipboard operation attempt {attempt + 1} of {maxRetries}");
            
            try
            {
                // キャンセル要求をチェック
                cancellationToken.ThrowIfCancellationRequested();

                // スレッドセーフにクリップボードにテキストを設定
                lock (_lockObject)
                {
                    System.Windows.Clipboard.SetText(text);
                }
                Debug.WriteLine($"Clipboard.SetText() executed successfully on attempt {attempt + 1}");

                // 設定後に短い遅延を入れてからクリップボードの内容を検証
                await Task.Delay(delayMs, cancellationToken);
                Debug.WriteLine($"Post-set delay completed on attempt {attempt + 1}");
                
                // 実際にクリップボードに正しく設定されたかを検証
                Debug.WriteLine($"Starting clipboard content verification on attempt {attempt + 1}");
                if (await VerifyClipboardContentAsync(text, cancellationToken))
                {
                    Debug.WriteLine($"✓ Clipboard text set successfully on attempt {attempt + 1}");
                    return true;
                }

                Debug.WriteLine($"✗ Clipboard verification failed on attempt {attempt + 1}");
                if (attempt < maxRetries - 1)
                {
                    Debug.WriteLine($"Will retry clipboard operation after delay (attempt {attempt + 2} will follow)");
                }
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x800401D0)) // CLIPBRD_E_CANT_OPEN
            {
                Debug.WriteLine($"⚠ Clipboard is locked (attempt {attempt + 1}): {ex.Message}");
                if (attempt < maxRetries - 1)
                {
                    int delayTime = delayMs * (attempt + 1);
                    Debug.WriteLine($"Retrying after {delayTime}ms delay due to clipboard lock (next attempt: {attempt + 2})");
                    // 指数バックオフ: 試行回数に応じて遅延時間を増加させる
                    // 1回目: 50ms * 1 = 50ms
                    // 2回目: 50ms * 2 = 100ms  
                    // 3回目: 50ms * 3 = 150ms
                    // これにより、他のアプリがクリップボードを解放する時間を確保
                    await Task.Delay(delayTime, cancellationToken);
                    Debug.WriteLine($"Retry delay completed, starting attempt {attempt + 2}");
                }
                else
                {
                    Debug.WriteLine($"✗ All attempts failed due to clipboard lock");
                }
            }
            catch (ExternalException ex)
            {
                Debug.WriteLine($"⚠ External exception during clipboard operation (attempt {attempt + 1}): {ex.Message}");
                if (attempt < maxRetries - 1)
                {
                    int delayTime = delayMs * (attempt + 1);
                    Debug.WriteLine($"Retrying after {delayTime}ms delay due to external exception (next attempt: {attempt + 2})");
                    // 外部例外の場合も指数バックオフで再試行
                    await Task.Delay(delayTime, cancellationToken);
                    Debug.WriteLine($"Retry delay completed, starting attempt {attempt + 2}");
                }
                else
                {
                    Debug.WriteLine($"✗ All attempts failed due to external exception");
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"⚠ Clipboard operation was cancelled on attempt {attempt + 1}");
                throw; // キャンセル例外は再スローして呼び出し元に通知
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠ Unexpected error setting clipboard text (attempt {attempt + 1}): {ex.Message}");
                if (attempt == maxRetries - 1)
                {
                    Debug.WriteLine($"✗ Final attempt failed, throwing exception");
                    throw; // 最後の試行で失敗した場合は例外を再スロー
                }
                // その他の例外でも指数バックオフで再試行
                int delayTime = delayMs * (attempt + 1);
                Debug.WriteLine($"Retrying after {delayTime}ms delay due to unexpected error (next attempt: {attempt + 2})");
                await Task.Delay(delayTime, cancellationToken);
                Debug.WriteLine($"Retry delay completed, starting attempt {attempt + 2}");
            }
        }

        // すべての試行が失敗した場合
        Debug.WriteLine($"✗ All {maxRetries} attempts to set clipboard text failed");
        return false;
    }

    /// <summary>
    /// クリップボードの内容が期待されるテキストと一致するかを検証します。
    /// 
    /// 検証の流れ：
    /// 1. 最大3回まで検証を試行
    /// 2. クリップボードにテキストが含まれているかチェック
    /// 3. 実際のテキストを取得して期待値と比較
    /// 4. 一致しない場合は短い遅延後に再試行
    /// 5. 例外が発生した場合も再試行を行う
    /// </summary>
    /// <param name="expectedText">期待されるテキスト</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>検証が成功したかどうか</returns>
    public async Task<bool> VerifyClipboardContentAsync(string expectedText, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;      // 最大検証試行回数
        const int delayMs = 25;        // 検証間の遅延時間（設定より短め）

        Debug.WriteLine($"Starting clipboard content verification. Expected text length: {expectedText?.Length ?? 0}");

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Debug.WriteLine($"Clipboard verification attempt {attempt + 1} of {maxRetries}");
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string actualText;
                // スレッドセーフにクリップボードの内容を取得
                lock (_lockObject)
                {
                    if (!System.Windows.Clipboard.ContainsText())
                    {
                        Debug.WriteLine($"✗ Clipboard does not contain text on verification attempt {attempt + 1}");
                        return false; // テキストが含まれていない場合は失敗
                    }
                    actualText = System.Windows.Clipboard.GetText();
                }
                Debug.WriteLine($"Retrieved clipboard text on verification attempt {attempt + 1}. Length: {actualText?.Length ?? 0}");

                // 期待値と実際の値を厳密に比較（大文字小文字も区別）
                if (string.Equals(actualText, expectedText, StringComparison.Ordinal))
                {
                    Debug.WriteLine($"✓ Clipboard content verification successful on attempt {attempt + 1}");
                    return true; // 検証成功
                }

                Debug.WriteLine($"✗ Clipboard content mismatch (attempt {attempt + 1}). Expected length: {expectedText?.Length}, Actual length: {actualText?.Length}");
                if (attempt < maxRetries - 1)
                {
                    Debug.WriteLine($"Will retry verification after {delayMs}ms delay (attempt {attempt + 2} will follow)");
                }
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x800401D0))
            {
                Debug.WriteLine($"⚠ Clipboard locked during verification (attempt {attempt + 1}): {ex.Message}");
                if (attempt < maxRetries - 1)
                {
                    Debug.WriteLine($"Retrying verification after {delayMs}ms delay due to clipboard lock (next attempt: {attempt + 2})");
                }
                else
                {
                    Debug.WriteLine($"✗ All verification attempts failed due to clipboard lock");
                }
            }
            catch (ExternalException ex)
            {
                Debug.WriteLine($"⚠ External exception during clipboard verification (attempt {attempt + 1}): {ex.Message}");
                if (attempt < maxRetries - 1)
                {
                    Debug.WriteLine($"Retrying verification after {delayMs}ms delay due to external exception (next attempt: {attempt + 2})");
                }
                else
                {
                    Debug.WriteLine($"✗ All verification attempts failed due to external exception");
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"⚠ Clipboard verification was cancelled on attempt {attempt + 1}");
                throw; // キャンセル例外は再スロー
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠ Unexpected error verifying clipboard content (attempt {attempt + 1}): {ex.Message}");
                if (attempt < maxRetries - 1)
                {
                    Debug.WriteLine($"Retrying verification after {delayMs}ms delay due to unexpected error (next attempt: {attempt + 2})");
                }
                else
                {
                    Debug.WriteLine($"✗ All verification attempts failed due to unexpected error");
                }
            }

            // 再試行前の短い遅延（検証なので設定より短い遅延）
            if (attempt < maxRetries - 1)
            {
                await Task.Delay(delayMs, cancellationToken);
                Debug.WriteLine($"Verification retry delay completed, starting attempt {attempt + 2}");
            }
        }

        // すべての検証試行が失敗
        Debug.WriteLine($"✗ All {maxRetries} clipboard verification attempts failed");
        return false;
    }

    /// <summary>
    /// クリップボードにテキストを設定します（同期版）。
    /// </summary>
    /// <param name="text">設定するテキスト</param>
    public void SetText(string text)
    {
        Debug.WriteLine($"Starting synchronous clipboard text setting. Text length: {text?.Length ?? 0}");
        
        try
        {
            lock (_lockObject)
            {
                System.Windows.Clipboard.SetText(text);
            }
            Debug.WriteLine($"✓ Synchronous clipboard text set successfully");
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x800401D0))
        {
            Debug.WriteLine($"✗ Clipboard is locked during synchronous operation: {ex.Message}");
            throw;
        }
        catch (ExternalException ex)
        {
            Debug.WriteLine($"✗ External exception during synchronous clipboard operation: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Unexpected error during synchronous clipboard operation: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// クリップボードからテキストを安全に取得します。
    /// </summary>
    /// <returns>クリップボードのテキスト、失敗した場合はnull</returns>
    public string GetTextSafely()
    {
        Debug.WriteLine($"Starting safe clipboard text retrieval");
        
        try
        {
            lock (_lockObject)
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    string text = System.Windows.Clipboard.GetText();
                    Debug.WriteLine($"✓ Successfully retrieved clipboard text. Length: {text?.Length ?? 0}");
                    return text;
                }
                else
                {
                    Debug.WriteLine($"⚠ Clipboard does not contain text");
                }
            }
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x800401D0))
        {
            Debug.WriteLine($"⚠ Clipboard is locked during text retrieval: {ex.Message}");
        }
        catch (ExternalException ex)
        {
            Debug.WriteLine($"⚠ External exception during clipboard text retrieval: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠ Unexpected error during clipboard text retrieval: {ex.Message}");
        }

        Debug.WriteLine($"✗ Failed to retrieve clipboard text, returning null");
        return null;
    }

    /// <summary>
    /// ウィンドウメッセージを処理するフックメソッド。
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            Debug.WriteLine($"📋 Clipboard update message received (WM_CLIPBOARDUPDATE)");
            
            try
            {
                string text = GetTextSafely();
                if (text != null)
                {
                    Debug.WriteLine($"✓ Clipboard update processed successfully. Notifying subscribers with text length: {text.Length}");
                    // イベントを発行して、購読者に通知
                    ClipboardUpdated?.Invoke(this, text);
                }
                else
                {
                    Debug.WriteLine($"⚠ Clipboard update detected but failed to retrieve text");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error in clipboard update handler: {ex.Message}");
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
            try
            {
                RemoveClipboardFormatListener(_hwndSource.Handle);
                _hwndSource.RemoveHook(WndProc);
                _hwndSource.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing ClipboardService: {ex.Message}");
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