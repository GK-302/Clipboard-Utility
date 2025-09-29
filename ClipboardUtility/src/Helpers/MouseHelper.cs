using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboardUtility.src.Helpers;

/// <summary>
/// マウスカーソルの位置など、マウスに関連する操作を提供する静的ヘルパークラスです。
/// </summary>
public static class MouseHelper
{
    /// <summary>
    /// Win32 APIから返される座標を格納するためのPOINT構造体。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public static implicit operator System.Drawing.Point(POINT point)
        {
            return new System.Drawing.Point(point.X, point.Y);
        }
    }

    /// <summary>
    /// Win32 APIのGetCursorPos関数をインポートします。
    /// これにより、現在のマウスカーソルのスクリーン座標を取得できます。
    /// </summary>
    /// <param name="lpPoint">座標を受け取るためのPOINT構造体へのポインタ</param>
    /// <returns>成功した場合はtrue、失敗した場合はfalse</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    /// 現在のマウスカーソルのスクリーン座標を取得します。
    /// </summary>
    /// <returns>現在のカーソル位置を表すSystem.Windows.Point</returns>
    public static System.Drawing.Point GetCursorPosition()
    {
        if (GetCursorPos(out POINT lpPoint))
        {
            // 暗黙的な型変換により、POINTをPointに変換して返す
            return lpPoint;
        }

        // 何らかの理由で取得に失敗した場合は、デフォルト値として(0,0)を返す
        return new System.Drawing.Point(0, 0);
    }
}