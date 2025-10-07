using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfSystemColors = System.Windows.SystemColors;

namespace ClipboardUtility.src.Helpers
{
    /// <summary>
    /// 色彩計算と背景色取得のヘルパークラス
    /// </summary>
    public static class ColorHelper
    {
        // Win32 API for getting pixel color
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// 指定された画面座標の背景色を取得します
        /// </summary>
        /// <param name="x">X座標（画面座標）</param>
        /// <param name="y">Y座標（画面座標）</param>
        /// <returns>背景色</returns>
        public static WpfColor GetBackgroundColor(int x, int y)
        {
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                {
                    Debug.WriteLine("ColorHelper: Failed to get device context");
                    return GetDefaultBackgroundColor();
                }

                try
                {
                    uint pixel = GetPixel(hdc, x, y);

                    // BGR形式からRGB形式に変換
                    byte r = (byte)(pixel & 0xFF);
                    byte g = (byte)((pixel >> 8) & 0xFF);
                    byte b = (byte)((pixel >> 16) & 0xFF);

                    WpfColor backgroundColor = WpfColor.FromRgb(r, g, b);
                    Debug.WriteLine($"ColorHelper: Background color at ({x}, {y}): R={r}, G={g}, B={b}");

                    return backgroundColor;
                }
                finally
                {
                    _ = ReleaseDC(IntPtr.Zero, hdc);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error getting background color: {ex.Message}");
                return GetDefaultBackgroundColor();
            }
        }

        /// <summary>
        /// 通知ウィンドウの位置の実際の画面ピクセル色を取得します（複数ポイントでサンプリング）
        /// 透明ウィンドウ用に最適化されており、ウィンドウが表示される実際の位置の背景色を検出します
        /// </summary>
        /// <param name="windowLeft">ウィンドウの左端座標</param>
        /// <param name="windowTop">ウィンドウの上端座標</param>
        /// <param name="windowWidth">ウィンドウの幅</param>
        /// <param name="windowHeight">ウィンドウの高さ</param>
        /// <returns>平均背景色</returns>
        public static WpfColor GetAverageBackgroundColor(double windowLeft, double windowTop, double windowWidth, double windowHeight)
        {
            try
            {
                Debug.WriteLine($"ColorHelper: Sampling screen pixel colors at notification position ({windowLeft}, {windowTop}) size {windowWidth}x{windowHeight}");

                // ウィンドウの実際の表示エリア内でサンプリングポイントを設定
                var samplePoints = new[]
                {
                    // ウィンドウの四隅
                    new WpfPoint(windowLeft + 5, windowTop + 5),
                    new WpfPoint(windowLeft + windowWidth - 5, windowTop + 5),
                    new WpfPoint(windowLeft + 5, windowTop + windowHeight - 5),
                    new WpfPoint(windowLeft + windowWidth - 5, windowTop + windowHeight - 5),
                    
                    // ウィンドウの各辺の中央
                    new WpfPoint(windowLeft + windowWidth / 2, windowTop + 5),
                    new WpfPoint(windowLeft + windowWidth / 2, windowTop + windowHeight - 5),
                    new WpfPoint(windowLeft + 5, windowTop + windowHeight / 2),
                    new WpfPoint(windowLeft + windowWidth - 5, windowTop + windowHeight / 2),
                    
                    // ウィンドウの中央
                    new WpfPoint(windowLeft + windowWidth / 2, windowTop + windowHeight / 2),
                    
                    // 追加のサンプリングポイント（より細かい色の検出）
                    new WpfPoint(windowLeft + windowWidth * 0.25, windowTop + windowHeight * 0.25),
                    new WpfPoint(windowLeft + windowWidth * 0.75, windowTop + windowHeight * 0.25),
                    new WpfPoint(windowLeft + windowWidth * 0.25, windowTop + windowHeight * 0.75),
                    new WpfPoint(windowLeft + windowWidth * 0.75, windowTop + windowHeight * 0.75)
                };

                int totalR = 0, totalG = 0, totalB = 0;
                int validSamples = 0;
                var colorSamples = new List<WpfColor>();

                foreach (var point in samplePoints)
                {
                    // 画面境界内かチェック
                    if (point.X >= 0 && point.Y >= 0 &&
                        point.X < SystemParameters.VirtualScreenWidth &&
                        point.Y < SystemParameters.VirtualScreenHeight)
                    {
                        WpfColor color = GetBackgroundColor((int)point.X, (int)point.Y);

                        // 有効な色の場合のみ計算に含める（完全な黒は除外しない - 実際の黒い背景の可能性）
                        colorSamples.Add(color);
                        totalR += color.R;
                        totalG += color.G;
                        totalB += color.B;
                        validSamples++;

                        Debug.WriteLine($"ColorHelper: Sample at ({point.X:F0}, {point.Y:F0}): R={color.R}, G={color.G}, B={color.B}");
                    }
                }

                if (validSamples > 0)
                {
                    // 基本的な平均色を計算
                    WpfColor averageColor = WpfColor.FromRgb(
                        (byte)(totalR / validSamples),
                        (byte)(totalG / validSamples),
                        (byte)(totalB / validSamples)
                    );

                    // 色のバリエーションをチェック（単色背景か複雑な背景かを判定）
                    double colorVariance = CalculateColorVariance(colorSamples);

                    Debug.WriteLine($"ColorHelper: Average screen color from {validSamples} samples: R={averageColor.R}, G={averageColor.G}, B={averageColor.B}, Variance={colorVariance:F2}");

                    // 色のバリエーションが大きい場合（複雑な背景）は、より中性的な色を返す
                    if (colorVariance > 50.0)
                    {
                        Debug.WriteLine("ColorHelper: High color variance detected, using median color for better text visibility");
                        return GetMedianColor(colorSamples);
                    }

                    return averageColor;
                }
                else
                {
                    Debug.WriteLine("ColorHelper: No valid samples found, using default background color");
                    return GetDefaultBackgroundColor();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error getting average screen color: {ex.Message}");
                return GetDefaultBackgroundColor();
            }
        }

        /// <summary>
        /// 色のバリエーション（分散）を計算します
        /// </summary>
        /// <param name="colors">色のリスト</param>
        /// <returns>色の分散値</returns>
        private static double CalculateColorVariance(List<WpfColor> colors)
        {
            if (colors.Count == 0) return 0;

            // 平均値を計算
            double avgR = colors.Average(c => c.R);
            double avgG = colors.Average(c => c.G);
            double avgB = colors.Average(c => c.B);

            // 分散を計算
            double variance = colors.Average(c =>
                Math.Pow(c.R - avgR, 2) +
                Math.Pow(c.G - avgG, 2) +
                Math.Pow(c.B - avgB, 2));

            return Math.Sqrt(variance);
        }

        /// <summary>
        /// 色のリストから中央値の色を取得します
        /// </summary>
        /// <param name="colors">色のリスト</param>
        /// <returns>中央値の色</returns>
        private static WpfColor GetMedianColor(List<WpfColor> colors)
        {
            if (colors.Count == 0) return GetDefaultBackgroundColor();

            var sortedR = colors.Select(c => c.R).OrderBy(x => x).ToList();
            var sortedG = colors.Select(c => c.G).OrderBy(x => x).ToList();
            var sortedB = colors.Select(c => c.B).OrderBy(x => x).ToList();

            int middle = colors.Count / 2;

            byte medianR = colors.Count % 2 == 0
                ? (byte)((sortedR[middle - 1] + sortedR[middle]) / 2)
                : sortedR[middle];

            byte medianG = colors.Count % 2 == 0
                ? (byte)((sortedG[middle - 1] + sortedG[middle]) / 2)
                : sortedG[middle];

            byte medianB = colors.Count % 2 == 0
                ? (byte)((sortedB[middle - 1] + sortedB[middle]) / 2)
                : sortedB[middle];

            return WpfColor.FromRgb(medianR, medianG, medianB);
        }

        /// <summary>
        /// 色の輝度を計算します（0.0-1.0の範囲）
        /// </summary>
        /// <param name="color">計算対象の色</param>
        /// <returns>輝度（0.0=黒、1.0=白）</returns>
        public static double CalculateLuminance(WpfColor color)
        {
            // sRGB色空間での相対輝度計算（ITU-R BT.709）
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            // ガンマ補正
            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            // 輝度計算
            double luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;

            Debug.WriteLine($"ColorHelper: Color R={color.R}, G={color.G}, B={color.B} has luminance {luminance:F3}");
            return luminance;
        }

        /// <summary>
        /// 2つの色のコントラスト比を計算します
        /// </summary>
        /// <param name="color1">色1</param>
        /// <param name="color2">色2</param>
        /// <returns>コントラスト比（1.0-21.0の範囲）</returns>
        public static double CalculateContrastRatio(WpfColor color1, WpfColor color2)
        {
            double luminance1 = CalculateLuminance(color1);
            double luminance2 = CalculateLuminance(color2);

            double lighter = Math.Max(luminance1, luminance2);
            double darker = Math.Min(luminance1, luminance2);

            double contrastRatio = (lighter + 0.05) / (darker + 0.05);

            Debug.WriteLine($"ColorHelper: Contrast ratio between colors: {contrastRatio:F2}");
            return contrastRatio;
        }

        /// <summary>
        /// 背景色に対して最適なテキスト色を決定します（透明ウィンドウ用に最適化）
        /// </summary>
        /// <param name="backgroundColor">背景色</param>
        /// <param name="preferredTextColor">希望するテキスト色（null の場合は自動選択）</param>
        /// <returns>最適なテキスト色</returns>
        public static WpfColor GetOptimalTextColor(WpfColor backgroundColor, WpfColor? preferredTextColor = null)
        {
            try
            {
                Debug.WriteLine($"ColorHelper: Determining optimal text color for screen background R={backgroundColor.R}, G={backgroundColor.G}, B={backgroundColor.B}");

                // 基本的な白と黒の候補
                WpfColor white = Colors.White;
                WpfColor black = Colors.Black;

                // より多くの候補色を用意（グレー系も含める）
                var candidates = new List<WpfColor> { white, black };

                // 希望する色が指定されている場合は追加
                if (preferredTextColor.HasValue)
                {
                    candidates.Add(preferredTextColor.Value);
                }

                // 背景色が中間的な場合のために、より明るい白とより暗い黒も候補に
                candidates.Add(WpfColor.FromRgb(255, 255, 255)); // 純白
                candidates.Add(WpfColor.FromRgb(0, 0, 0));       // 純黒
                candidates.Add(WpfColor.FromRgb(240, 240, 240)); // 薄いグレー
                candidates.Add(WpfColor.FromRgb(32, 32, 32));    // 濃いグレー

                WpfColor bestColor = white;
                double bestContrast = 0;

                foreach (WpfColor candidate in candidates)
                {
                    double contrast = CalculateContrastRatio(backgroundColor, candidate);

                    if (contrast > bestContrast)
                    {
                        bestContrast = contrast;
                        bestColor = candidate;
                    }
                }

                // WCAG AAの推奨最小コントラスト比をチェック
                string contrastLevel = bestContrast >= 7.0 ? "AAA" :
                                      bestContrast >= 4.5 ? "AA" :
                                      bestContrast >= 3.0 ? "AA Large" : "Poor";

                Debug.WriteLine($"ColorHelper: Selected text color R={bestColor.R}, G={bestColor.G}, B={bestColor.B} with contrast ratio {bestContrast:F2} ({contrastLevel})");

                return bestColor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error determining optimal text color: {ex.Message}");
                return Colors.White; // フォールバック
            }
        }

        /// <summary>
        /// テキストの縁取り（アウトライン）用の色を取得します
        /// </summary>
        /// <param name="textColor">テキストの色</param>
        /// <param name="backgroundColor">背景色</param>
        /// <returns>縁取り用の色</returns>
        public static WpfColor GetOutlineColor(WpfColor textColor, WpfColor backgroundColor)
        {
            try
            {
                // テキストが白系の場合は黒い縁取り、黒系の場合は白い縁取り
                double textLuminance = CalculateLuminance(textColor);

                WpfColor outlineColor;
                if (textLuminance > 0.5)
                {
                    // 明るいテキストには暗い縁取り
                    outlineColor = WpfColor.FromRgb(0, 0, 0);
                }
                else
                {
                    // 暗いテキストには明るい縁取り
                    outlineColor = WpfColor.FromRgb(255, 255, 255);
                }

                // 縁取り色と背景色のコントラストもチェック
                double outlineBackgroundContrast = CalculateContrastRatio(outlineColor, backgroundColor);

                Debug.WriteLine($"ColorHelper: Outline color R={outlineColor.R}, G={outlineColor.G}, B={outlineColor.B} with background contrast {outlineBackgroundContrast:F2}");

                return outlineColor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error determining outline color: {ex.Message}");
                // フォールバック: テキストが明るければ黒、暗ければ白
                double textLuminance = CalculateLuminance(textColor);
                return textLuminance > 0.5 ? Colors.Black : Colors.White;
            }
        }

        /// <summary>
        /// デフォルトの背景色を取得します（システムテーマに基づく）
        /// </summary>
        /// <returns>デフォルト背景色</returns>
        public static WpfColor GetDefaultBackgroundColor()
        {
            try
            {
                // システムの背景色を取得
                var systemColor = WpfSystemColors.DesktopColor;
                Debug.WriteLine($"ColorHelper: Using system desktop color as default: R={systemColor.R}, G={systemColor.G}, B={systemColor.B}");
                return systemColor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error getting system desktop color: {ex.Message}");
                // 最終フォールバック
                return WpfColor.FromRgb(240, 240, 240); // 薄いグレー
            }
        }

        /// <summary>
        /// 色を SolidColorBrush に変換します
        /// </summary>
        /// <param name="color">変換する色</param>
        /// <returns>SolidColorBrush</returns>
        public static SolidColorBrush ToBrush(this WpfColor color)
        {
            return new SolidColorBrush(color);
        }
    }
}