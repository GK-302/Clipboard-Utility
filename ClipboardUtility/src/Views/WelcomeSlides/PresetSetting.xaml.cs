using ClipboardUtility.src.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;

namespace ClipboardUtility.src.Views.WelcomeSlides;

/// <summary>
/// PresetSetting.xaml の相互作用ロジック
/// </summary>
public partial class PresetSetting : UserControl
{
    public PresetSetting()
    {
        InitializeComponent();
    }
    // 1. 依存関係プロパティの定義
    public static readonly DependencyProperty SelectedPresetProperty =
        DependencyProperty.Register(
            "SelectedPreset", // プロパティ名
            typeof(PresetType), // プロパティの型
            typeof(PresetSetting), // 所有者の型
            new FrameworkPropertyMetadata(
                PresetType.None, // デフォルト値
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)); // TwoWayバインディングを許可

    // 2. CLRラッパープロパティ
    public PresetType SelectedPreset
    {
        get { return (PresetType)GetValue(SelectedPresetProperty); }
        set { SetValue(SelectedPresetProperty, value); }
    }
}
