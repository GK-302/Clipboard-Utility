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
    /// �F�ʌv�Z�Ɣw�i�F�擾�̃w���p�[�N���X
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
        /// �w�肳�ꂽ��ʍ��W�̔w�i�F���擾���܂�
        /// </summary>
        /// <param name="x">X���W�i��ʍ��W�j</param>
        /// <param name="y">Y���W�i��ʍ��W�j</param>
        /// <returns>�w�i�F</returns>
        public static WpfColor GetBackgroundColor(int x, int y)
        {
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                {
                    //Debug.WriteLine("ColorHelper: Failed to get device context");
                    return GetDefaultBackgroundColor();
                }

                try
                {
                    uint pixel = GetPixel(hdc, x, y);

                    // BGR�`������RGB�`���ɕϊ�
                    byte r = (byte)(pixel & 0xFF);
                    byte g = (byte)((pixel >> 8) & 0xFF);
                    byte b = (byte)((pixel >> 16) & 0xFF);

                    WpfColor backgroundColor = WpfColor.FromRgb(r, g, b);
                    //Debug.WriteLine($"ColorHelper: Background color at ({x}, {y}): R={r}, G={g}, B={b}");

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
        /// �ʒm�E�B���h�E�̈ʒu�̎��ۂ̉�ʃs�N�Z���F���擾���܂��i�����|�C���g�ŃT���v�����O�j
        /// �����E�B���h�E�p�ɍœK������Ă���A�E�B���h�E���\���������ۂ̈ʒu�̔w�i�F�����o���܂�
        /// </summary>
        /// <param name="windowLeft">�E�B���h�E�̍��[���W</param>
        /// <param name="windowTop">�E�B���h�E�̏�[���W</param>
        /// <param name="windowWidth">�E�B���h�E�̕�</param>
        /// <param name="windowHeight">�E�B���h�E�̍���</param>
        /// <returns>���ϔw�i�F</returns>
        public static WpfColor GetAverageBackgroundColor(double windowLeft, double windowTop, double windowWidth, double windowHeight)
        {
            try
            {
                Debug.WriteLine($"ColorHelper: Sampling screen pixel colors at notification position ({windowLeft}, {windowTop}) size {windowWidth}x{windowHeight}");

                // �E�B���h�E�̎��ۂ̕\���G���A���ŃT���v�����O�|�C���g��ݒ�
                var samplePoints = new[]
                {
                    // �E�B���h�E�̎l��
                    new WpfPoint(windowLeft + 5, windowTop + 5),
                    new WpfPoint(windowLeft + windowWidth - 5, windowTop + 5),
                    new WpfPoint(windowLeft + 5, windowTop + windowHeight - 5),
                    new WpfPoint(windowLeft + windowWidth - 5, windowTop + windowHeight - 5),
                    
                    // �E�B���h�E�̊e�ӂ̒���
                    new WpfPoint(windowLeft + windowWidth / 2, windowTop + 5),
                    new WpfPoint(windowLeft + windowWidth / 2, windowTop + windowHeight - 5),
                    new WpfPoint(windowLeft + 5, windowTop + windowHeight / 2),
                    new WpfPoint(windowLeft + windowWidth - 5, windowTop + windowHeight / 2),
                    
                    // �E�B���h�E�̒���
                    new WpfPoint(windowLeft + windowWidth / 2, windowTop + windowHeight / 2),
                    
                    // �ǉ��̃T���v�����O�|�C���g�i���ׂ����F�̌��o�j
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
                    // ��ʋ��E�����`�F�b�N
                    if (point.X >= 0 && point.Y >= 0 &&
                        point.X < SystemParameters.VirtualScreenWidth &&
                        point.Y < SystemParameters.VirtualScreenHeight)
                    {
                        WpfColor color = GetBackgroundColor((int)point.X, (int)point.Y);

                        // �L���ȐF�̏ꍇ�̂݌v�Z�Ɋ܂߂�i���S�ȍ��͏��O���Ȃ� - ���ۂ̍����w�i�̉\���j
                        colorSamples.Add(color);
                        totalR += color.R;
                        totalG += color.G;
                        totalB += color.B;
                        validSamples++;

                        //Debug.WriteLine($"ColorHelper: Sample at ({point.X:F0}, {point.Y:F0}): R={color.R}, G={color.G}, B={color.B}");
                    }
                }

                if (validSamples > 0)
                {
                    // ��{�I�ȕ��ϐF���v�Z
                    WpfColor averageColor = WpfColor.FromRgb(
                        (byte)(totalR / validSamples),
                        (byte)(totalG / validSamples),
                        (byte)(totalB / validSamples)
                    );

                    // �F�̃o���G�[�V�������`�F�b�N�i�P�F�w�i�����G�Ȕw�i���𔻒�j
                    double colorVariance = CalculateColorVariance(colorSamples);

                    Debug.WriteLine($"ColorHelper: Average screen color from {validSamples} samples: R={averageColor.R}, G={averageColor.G}, B={averageColor.B}, Variance={colorVariance:F2}");

                    // �F�̃o���G�[�V�������傫���ꍇ�i���G�Ȕw�i�j�́A��蒆���I�ȐF��Ԃ�
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
        /// �F�̃o���G�[�V�����i���U�j���v�Z���܂�
        /// </summary>
        /// <param name="colors">�F�̃��X�g</param>
        /// <returns>�F�̕��U�l</returns>
        private static double CalculateColorVariance(List<WpfColor> colors)
        {
            if (colors.Count == 0) return 0;

            // ���ϒl���v�Z
            double avgR = colors.Average(c => c.R);
            double avgG = colors.Average(c => c.G);
            double avgB = colors.Average(c => c.B);

            // ���U���v�Z
            double variance = colors.Average(c =>
                Math.Pow(c.R - avgR, 2) +
                Math.Pow(c.G - avgG, 2) +
                Math.Pow(c.B - avgB, 2));

            return Math.Sqrt(variance);
        }

        /// <summary>
        /// �F�̃��X�g���璆���l�̐F���擾���܂�
        /// </summary>
        /// <param name="colors">�F�̃��X�g</param>
        /// <returns>�����l�̐F</returns>
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
        /// �F�̋P�x���v�Z���܂��i0.0-1.0�͈̔́j
        /// </summary>
        /// <param name="color">�v�Z�Ώۂ̐F</param>
        /// <returns>�P�x�i0.0=���A1.0=���j</returns>
        public static double CalculateLuminance(WpfColor color)
        {
            // sRGB�F��Ԃł̑��΋P�x�v�Z�iITU-R BT.709�j
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            // �K���}�␳
            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            // �P�x�v�Z
            double luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;

            //Debug.WriteLine($"ColorHelper: Color R={color.R}, G={color.G}, B={color.B} has luminance {luminance:F3}");
            return luminance;
        }

        /// <summary>
        /// 2�̐F�̃R���g���X�g����v�Z���܂�
        /// </summary>
        /// <param name="color1">�F1</param>
        /// <param name="color2">�F2</param>
        /// <returns>�R���g���X�g��i1.0-21.0�͈̔́j</returns>
        public static double CalculateContrastRatio(WpfColor color1, WpfColor color2)
        {
            double luminance1 = CalculateLuminance(color1);
            double luminance2 = CalculateLuminance(color2);

            double lighter = Math.Max(luminance1, luminance2);
            double darker = Math.Min(luminance1, luminance2);

            double contrastRatio = (lighter + 0.05) / (darker + 0.05);

            //Debug.WriteLine($"ColorHelper: Contrast ratio between colors: {contrastRatio:F2}");
            return contrastRatio;
        }

        /// <summary>
        /// �w�i�F�ɑ΂��čœK�ȃe�L�X�g�F�����肵�܂��i�����E�B���h�E�p�ɍœK���j
        /// </summary>
        /// <param name="backgroundColor">�w�i�F</param>
        /// <param name="preferredTextColor">��]����e�L�X�g�F�inull �̏ꍇ�͎����I���j</param>
        /// <returns>�œK�ȃe�L�X�g�F</returns>
        public static WpfColor GetOptimalTextColor(WpfColor backgroundColor, WpfColor? preferredTextColor = null)
        {
            try
            {
                //Debug.WriteLine($"ColorHelper: Determining optimal text color for screen background R={backgroundColor.R}, G={backgroundColor.G}, B={backgroundColor.B}");

                // ��{�I�Ȕ��ƍ��̌��
                WpfColor white = Colors.White;
                WpfColor black = Colors.Black;

                // ��葽���̌��F��p�Ӂi�O���[�n���܂߂�j
                var candidates = new List<WpfColor> { white, black };

                // ��]����F���w�肳��Ă���ꍇ�͒ǉ�
                if (preferredTextColor.HasValue)
                {
                    candidates.Add(preferredTextColor.Value);
                }

                // �w�i�F�����ԓI�ȏꍇ�̂��߂ɁA��薾�邢���Ƃ��Â���������
                candidates.Add(WpfColor.FromRgb(255, 255, 255)); // ����
                candidates.Add(WpfColor.FromRgb(0, 0, 0));       // ����
                candidates.Add(WpfColor.FromRgb(240, 240, 240)); // �����O���[
                candidates.Add(WpfColor.FromRgb(32, 32, 32));    // �Z���O���[

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

                // WCAG AA�̐����ŏ��R���g���X�g����`�F�b�N
                string contrastLevel = bestContrast >= 7.0 ? "AAA" :
                                      bestContrast >= 4.5 ? "AA" :
                                      bestContrast >= 3.0 ? "AA Large" : "Poor";

                //Debug.WriteLine($"ColorHelper: Selected text color R={bestColor.R}, G={bestColor.G}, B={bestColor.B} with contrast ratio {bestContrast:F2} ({contrastLevel})");

                return bestColor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error determining optimal text color: {ex.Message}");
                return Colors.White; // �t�H�[���o�b�N
            }
        }

        /// <summary>
        /// �e�L�X�g�̉����i�A�E�g���C���j�p�̐F���擾���܂�
        /// </summary>
        /// <param name="textColor">�e�L�X�g�̐F</param>
        /// <param name="backgroundColor">�w�i�F</param>
        /// <returns>�����p�̐F</returns>
        public static WpfColor GetOutlineColor(WpfColor textColor, WpfColor backgroundColor)
        {
            try
            {
                // �e�L�X�g�����n�̏ꍇ�͍��������A���n�̏ꍇ�͔��������
                double textLuminance = CalculateLuminance(textColor);

                WpfColor outlineColor;
                if (textLuminance > 0.5)
                {
                    // ���邢�e�L�X�g�ɂ͈Â������
                    outlineColor = WpfColor.FromRgb(0, 0, 0);
                }
                else
                {
                    // �Â��e�L�X�g�ɂ͖��邢�����
                    outlineColor = WpfColor.FromRgb(255, 255, 255);
                }

                // �����F�Ɣw�i�F�̃R���g���X�g���`�F�b�N
                double outlineBackgroundContrast = CalculateContrastRatio(outlineColor, backgroundColor);

                //Debug.WriteLine($"ColorHelper: Outline color R={outlineColor.R}, G={outlineColor.G}, B={outlineColor.B} with background contrast {outlineBackgroundContrast:F2}");

                return outlineColor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error determining outline color: {ex.Message}");
                // �t�H�[���o�b�N: �e�L�X�g�����邯��΍��A�Â���Δ�
                double textLuminance = CalculateLuminance(textColor);
                return textLuminance > 0.5 ? Colors.Black : Colors.White;
            }
        }

        /// <summary>
        /// �f�t�H���g�̔w�i�F���擾���܂��i�V�X�e���e�[�}�Ɋ�Â��j
        /// </summary>
        /// <returns>�f�t�H���g�w�i�F</returns>
        public static WpfColor GetDefaultBackgroundColor()
        {
            try
            {
                // �V�X�e���̔w�i�F���擾
                var systemColor = WpfSystemColors.DesktopColor;
                Debug.WriteLine($"ColorHelper: Using system desktop color as default: R={systemColor.R}, G={systemColor.G}, B={systemColor.B}");
                return systemColor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ColorHelper: Error getting system desktop color: {ex.Message}");
                // �ŏI�t�H�[���o�b�N
                return WpfColor.FromRgb(240, 240, 240); // �����O���[
            }
        }

        /// <summary>
        /// �F�� SolidColorBrush �ɕϊ����܂�
        /// </summary>
        /// <param name="color">�ϊ�����F</param>
        /// <returns>SolidColorBrush</returns>
        public static SolidColorBrush ToBrush(this WpfColor color)
        {
            return new SolidColorBrush(color);
        }
    }
}