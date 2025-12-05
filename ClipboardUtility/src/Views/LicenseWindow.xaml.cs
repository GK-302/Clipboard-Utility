using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using GroupBox = System.Windows.Controls.GroupBox;
using Path = System.IO.Path;
using TextBox = System.Windows.Controls.TextBox;

namespace ClipboardUtility.src.Views
{
    /// <summary>
    /// LicenseWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LicenseWindow : Window
    {
        private readonly LicenseLoaderService _licenseLoader;

        public LicenseWindow()
        {
            InitializeComponent();
            _licenseLoader = new LicenseLoaderService();
            LoadAllLicenses();
            Debug.WriteLine("LicenseWindow initialized.");
        }

        private void LoadAllLicenses()
        {
            LoadPackageLicenses();
            LoadFontLicenses();
        }

        private void LoadPackageLicenses()
        {
            try
            {
                var licenses = _licenseLoader.LoadPackageLicenses();

                if (licenses.Count == 0)
                {
                    PackageLicenseStackPanel.Children.Add(new TextBlock
                    {
                        Text = "パッケージライセンス情報が見つかりませんでした。\n\n" +
                               "licenses.json がビルド出力ディレクトリに存在しない可能性があります。\n" +
                               "dotnet-project-licenses ツールがインストールされているか確認してください。",
                        Margin = new Thickness(8),
                        TextWrapping = TextWrapping.Wrap
                    });
                    return;
                }

                foreach (var license in licenses.OrderBy(l => l.PackageName))
                {
                    AddPackageLicenseBlock(license);
                }

                Debug.WriteLine($"Loaded {licenses.Count} package license(s).");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load package licenses: {ex}");
                PackageLicenseStackPanel.Children.Add(new TextBlock
                {
                    Text = "パッケージライセンス情報の読み込みに失敗しました。\n\n" + ex.Message,
                    Margin = new Thickness(8),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        private void LoadFontLicenses()
        {
            try
            {
                var fontLicenses = _licenseLoader.LoadFontLicenses();

                if (fontLicenses.Count == 0)
                {
                    FontLicenseStackPanel.Children.Add(new TextBlock
                    {
                        Text = "フォントライセンス情報が見つかりませんでした。",
                        Margin = new Thickness(8),
                        TextWrapping = TextWrapping.Wrap
                    });
                    return;
                }

                foreach (var (title, content) in fontLicenses.OrderBy(kv => kv.Key))
                {
                    AddFontLicenseBlock(title, content);
                }

                Debug.WriteLine($"Loaded {fontLicenses.Count} font license(s).");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load font licenses: {ex}");
                FontLicenseStackPanel.Children.Add(new TextBlock
                {
                    Text = "フォントライセンス情報の読み込みに失敗しました。\n\n" + ex.Message,
                    Margin = new Thickness(8),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        private void AddPackageLicenseBlock(LicenseInfo license)
        {
            var group = new GroupBox
            {
                Header = license.DisplayName,
                Margin = new Thickness(8)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(8) };

            // License Type
            if (!string.IsNullOrWhiteSpace(license.LicenseType))
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"ライセンス: {license.LicenseType}",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }

            // Authors
            if (!string.IsNullOrWhiteSpace(license.AuthorsDisplay))
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"著者: {license.AuthorsDisplay}",
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }

            // Copyright
            if (!string.IsNullOrWhiteSpace(license.Copyright))
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = license.Copyright,
                    Margin = new Thickness(0, 0, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            // Description
            if (!string.IsNullOrWhiteSpace(license.Description))
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = license.Description,
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Colors.Gray)
                });
            }

            // Links
            var linksPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };

            if (!string.IsNullOrWhiteSpace(license.PackageUrl))
            {
                var packageLink = CreateHyperlink("パッケージURL", license.PackageUrl);
                linksPanel.Children.Add(packageLink);
            }

            if (!string.IsNullOrWhiteSpace(license.LicenseUrl))
            {
                var licenseLink = CreateHyperlink("ライセンスURL", license.LicenseUrl);
                linksPanel.Children.Add(licenseLink);
            }

            if (linksPanel.Children.Count > 0)
            {
                stackPanel.Children.Add(linksPanel);
            }

            group.Content = stackPanel;
            PackageLicenseStackPanel.Children.Add(group);
        }

        private void AddFontLicenseBlock(string title, string content)
        {
            var group = new GroupBox
            {
                Header = title,
                Margin = new Thickness(8)
            };

            var textBox = new TextBox
            {
                Text = content,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                AcceptsReturn = true,
                MinHeight = 160
            };

            group.Content = textBox;
            FontLicenseStackPanel.Children.Add(group);
        }

        private UIElement CreateHyperlink(string text, string url)
        {
            var textBlock = new TextBlock { Margin = new Thickness(0, 0, 16, 0) };
            var hyperlink = new Hyperlink
            {
                NavigateUri = new Uri(url)
            };
            hyperlink.Inlines.Add(text);
            hyperlink.RequestNavigate += (sender, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = e.Uri.AbsoluteUri,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to open URL: {ex.Message}");
                }
                e.Handled = true;
            };
            textBlock.Inlines.Add(hyperlink);
            return textBlock;
        }
    }
}
