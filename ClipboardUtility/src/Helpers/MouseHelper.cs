using System.Runtime.InteropServices;
using System.Windows;
// System.Drawing と System.Windows.Forms への参照が必要です

namespace ClipboardUtility.src.Helpers;

/// <summary>
/// マウスカーソルの位置など、マウスに関連する操作を提供する静的ヘルパークラスです。
/// </summary>
public static class MouseHelper
{
    // (POINT構造体とP/Invoke宣言は変更なしのため省略... 元のコードと同じものを配置してください)
    #region P/Invoke Declarations
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
        public static implicit operator System.Drawing.Point(POINT point) => new(point.X, point.Y);
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private enum MonitorDpiType { MDT_EFFECTIVE_DPI = 0, MDT_ANGULAR_DPI = 1, MDT_RAW_DPI = 2 }
    [DllImport("Shcore.dll", SetLastError = true)]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);
    #endregion

    /// <summary>
    /// 現在のマウスカーソルのスクリーン座標（物理ピクセル）を取得します。
    /// </summary>
    public static System.Drawing.Point GetCursorPosition()
    {
        if (GetCursorPos(out POINT lpPoint))
        {
            return lpPoint;
        }
        return new System.Drawing.Point(0, 0);
    }

    /// <summary>
    /// DPIを考慮し、かつウィンドウが画面からはみ出さないように補正したカーソル座標を取得します。
    /// </summary>
    /// <param name="windowWidth">表示するウィンドウの幅（論理ピクセル）</param>
    /// <param name="windowHeight">表示するウィンドウの高さ（論理ピクセル）</param>
    /// <param name="offsetX">カーソルからの水平オフセット（論理ピクセル）</param>
    /// <param name="offsetY">カーソルからの垂直オフセット（論理ピクセル）</param>
    /// <returns>画面内に収まるように補正された、ウィンドウの左上隅の座標（論理ピクセル）</returns>
    public static System.Windows.Point GetClampedPosition(Window window, int offsetX = 0, int offsetY = 0)
    {
        try
        {
            // 1. カーソル位置、モニター情報、DPIスケールを取得
            var cursorPhysicalPoint = GetCursorPosition();
            var monitorHandle = MonitorFromPoint(new POINT { X = cursorPhysicalPoint.X, Y = cursorPhysicalPoint.Y }, MONITOR_DEFAULTTONEAREST);

            double scaleX = 1.0;
            double scaleY = 1.0;

            if (GetDpiForMonitor(monitorHandle, MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY) == 0) // S_OKは0
            {
                scaleX = dpiX / 96.0;
                scaleY = dpiY / 96.0;
            }
            else // フォールバック
            {
                using var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
                scaleX = g.DpiX / 96.0;
                scaleY = g.DpiY / 96.0;
            }

            // 2. カーソルがあるモニターの作業領域を「物理ピクセル」で取得
            var screen = Screen.FromPoint(cursorPhysicalPoint);
            var workingArea = screen.WorkingArea;

            // 3. ウィンドウサイズとオフセットを「物理ピクセル」に変換
            double windowWidthInPixels = window.Width * scaleX;
            double windowHeightInPixels = window.Height * scaleY;
            double offsetXInPixels = offsetX * scaleX;
            double offsetYInPixels = offsetY * scaleY;

            // 4. ウィンドウの理想的な表示位置を「物理ピクセル」で計算
            double targetX = cursorPhysicalPoint.X + offsetXInPixels;
            double targetY = cursorPhysicalPoint.Y + offsetYInPixels;

            // 5. 見切れ防止の補正（クランプ処理）
            // 右端にはみ出す場合、ウィンドウの右端を作業領域の右端に合わせる
            if (targetX + windowWidthInPixels > workingArea.Right)
            {
                targetX = workingArea.Right - windowWidthInPixels;
            }
            // 下端にはみ出す場合、ウィンドウの下端を作業領域の下端に合わせる
            if (targetY + windowHeightInPixels > workingArea.Bottom)
            {
                targetY = workingArea.Bottom - windowHeightInPixels;
            }
            // 左端にはみ出す場合、ウィンドウの左端を作業領域の左端に合わせる
            if (targetX < workingArea.Left)
            {
                targetX = workingArea.Left;
            }
            // 上端にはみ出す場合、ウィンドウの上端を作業領域の上端に合わせる
            if (targetY < workingArea.Top)
            {
                targetY = workingArea.Top;
            }

            // 6. 最終的な物理座標をWPFの「論理ピクセル」に変換して返す
            return new System.Windows.Point(targetX / scaleX, targetY / scaleY);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during clamped position calculation: {ex.Message}");
            return new System.Windows.Point(0, 0);
        }
    }
}