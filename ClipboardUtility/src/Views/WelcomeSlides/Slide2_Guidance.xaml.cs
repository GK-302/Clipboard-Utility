using System.Windows; // DependencyPropertyChangedEventArgs のために追加
using System.Windows.Controls;
using System.Windows.Media.Animation; // Storyboard のために追加
using UserControl = System.Windows.Controls.UserControl;

namespace ClipboardUtility.src.Views.WelcomeSlides;

public partial class Slide2_Guidance : UserControl
{
    // 2種類のアニメーションを保持するフィールド
    private Storyboard? _textFadeInAnimation;

    public Slide2_Guidance()
    {
        InitializeComponent();

        // XAMLリソースからストーリーボードを取得
        _textFadeInAnimation = this.FindResource("TextFadeInAnimation") as Storyboard;
    }

    private void Slide2_Guidance_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // (bool)e.NewValue が true (表示された) で、
        // かつ まだアニメーションが再生されていない場合
        if ((bool)e.NewValue)
        {
            // アニメーションを開始
            _textFadeInAnimation?.Begin(this); // テキストのフェードインも開始
        }
    }
}