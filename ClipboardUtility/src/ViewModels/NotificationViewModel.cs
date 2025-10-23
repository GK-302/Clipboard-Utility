// NotificationViewModel.cs
using ClipboardUtility.src.Helpers;
using System.ComponentModel;
using System.Diagnostics;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;

namespace ClipboardUtility.src.ViewModels;

/// <summary>
/// 通知用のViewModel。通知メッセージを保持し、変更をUIに通知します。
/// 透明ウィンドウで実際の画面背景色に基づいて最適なテキスト色を自動調整する機能を含みます。
/// </summary>
public class NotificationViewModel : INotifyPropertyChanged
{
    private string _notificationMessage;
    private string _notificationsimultaneousMessage;
    private WpfColor _backgroundColor;
    private WpfBrush _textBrush;
    private WpfColor _outlineColor;

    public NotificationViewModel()
    {
        // デフォルト色を設定
        SetColorsForScreenBackground(ColorHelper.GetDefaultBackgroundColor());
    }

    // UIがバインドする対象となるプロパティ
    public string NotificationMessage
    {
        get => _notificationMessage;
        set
        {
            _notificationMessage = value;
            OnPropertyChanged(nameof(NotificationMessage));
        }
    }

    public string NotificationsimultaneousMessage
    {
        get => _notificationsimultaneousMessage;
        set
        {
            _notificationsimultaneousMessage = value;
            OnPropertyChanged(nameof(NotificationsimultaneousMessage));
        }
    }

    /// <summary>
    /// 実際の画面背景色（検出された色）
    /// </summary>
    public WpfColor BackgroundColor
    {
        get => _backgroundColor;
        private set
        {
            _backgroundColor = value;
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    /// <summary>
    /// テキスト用のブラシ（画面背景色に基づいて自動調整）
    /// </summary>
    public WpfBrush TextBrush
    {
        get => _textBrush;
        private set
        {
            _textBrush = value;
            OnPropertyChanged(nameof(TextBrush));
        }
    }

    /// <summary>
    /// テキストの縁取り色（視認性向上のため）
    /// </summary>
    public WpfColor OutlineColor
    {
        get => _outlineColor;
        private set
        {
            _outlineColor = value;
            OnPropertyChanged(nameof(OutlineColor));
        }
    }

    /// <summary>
    /// 画面背景色に基づいてテキスト色と縁取り色を設定します
    /// </summary>
    /// <param name="screenBackgroundColor">検出された画面背景色</param>
    /// <param name="preferredTextColor">希望するテキスト色（nullの場合は自動選択）</param>
    public void SetColorsForScreenBackground(WpfColor screenBackgroundColor, WpfColor? preferredTextColor = null)
    {
        try
        {
            //Debug.WriteLine($"NotificationViewModel: Setting colors for screen background R={screenBackgroundColor.R}, G={screenBackgroundColor.G}, B={screenBackgroundColor.B}");

            BackgroundColor = screenBackgroundColor;

            // 画面背景色に基づいて最適なテキスト色を決定
            WpfColor optimalTextColor = ColorHelper.GetOptimalTextColor(screenBackgroundColor, preferredTextColor);
            TextBrush = optimalTextColor.ToBrush();

            // テキストの縁取り色を決定（視認性を向上させるため）
            WpfColor outlineColor = ColorHelper.GetOutlineColor(optimalTextColor, screenBackgroundColor);
            OutlineColor = outlineColor;

            //Debug.WriteLine($"NotificationViewModel: Text color set to R={optimalTextColor.R}, G={optimalTextColor.G}, B={optimalTextColor.B}");
            //Debug.WriteLine($"NotificationViewModel: Outline color set to R={outlineColor.R}, G={outlineColor.G}, B={outlineColor.B}");
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"NotificationViewModel: Error setting colors for screen background: {ex.Message}");

            // エラー時のフォールバック
            BackgroundColor = System.Windows.Media.Colors.Gray;
            TextBrush = System.Windows.Media.Brushes.White;
            OutlineColor = System.Windows.Media.Colors.Black;
        }
    }

    /// <summary>
    /// 通知ウィンドウの位置に基づいて画面の実際のピクセル色を検出し、テキスト色を調整します
    /// </summary>
    /// <param name="windowLeft">ウィンドウの左端座標</param>
    /// <param name="windowTop">ウィンドウの上端座標</param>
    /// <param name="windowWidth">ウィンドウの幅</param>
    /// <param name="windowHeight">ウィンドウの高さ</param>
    public void AutoAdjustColorsForScreenPosition(double windowLeft, double windowTop, double windowWidth, double windowHeight)
    {
        try
        {
            Debug.WriteLine($"NotificationViewModel: Auto-adjusting colors for screen position ({windowLeft}, {windowTop}) size {windowWidth}x{windowHeight}");

            // ウィンドウ位置の実際の画面ピクセル色を取得
            WpfColor detectedScreenColor = ColorHelper.GetAverageBackgroundColor(windowLeft, windowTop, windowWidth, windowHeight);

            // 検出した画面色に基づいて色を設定
            SetColorsForScreenBackground(detectedScreenColor);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"NotificationViewModel: Error auto-adjusting colors for screen position: {ex.Message}");

            // エラー時はデフォルト色を使用
            SetColorsForScreenBackground(ColorHelper.GetDefaultBackgroundColor());
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}