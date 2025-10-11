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
        public LicenseWindow()
        {
            InitializeComponent();
            LoadLicenseText();
            Debug.WriteLine("LicenseWindow initialized.");
        }
        private void LoadLicenseText()
        {
            try
            {
                // 1) Try to find OFL.txt files in the application's output directory (supports adding licenses later)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
                var foundFiles = new List<string>();

                try
                {
                    if (Directory.Exists(baseDir))
                    {
                        // Search for files named OFL.txt under the output directory
                        foundFiles = Directory.EnumerateFiles(baseDir, "OFL.txt", SearchOption.AllDirectories).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while searching for license files on disk: {ex}");
                }

                if (foundFiles.Count > 0)
                {
                    Debug.WriteLine($"Found {foundFiles.Count} license files on disk.");
                    foreach (var file in foundFiles)
                    {
                        string title = Path.GetFileName(Path.GetDirectoryName(file)) ?? Path.GetFileName(file);
                        string content = File.ReadAllText(file, Encoding.UTF8);
                        AddLicenseBlock(title, content);
                    }
                    return;
                }

                // 2) Fallback: try known pack URIs (for resources marked as Resource)
                var knownDirs = new[] { "Roboto", "NotoSansJP" };
                foreach (var dir in knownDirs)
                {
                    var uri = new Uri($"pack://application:,,,/ClipboardUtility;component/src/Assets/Fonts/{dir}/OFL.txt", UriKind.Absolute);
                    var streamInfo = System.Windows.Application.GetResourceStream(uri);
                    if (streamInfo == null)
                    {
                        Debug.WriteLine($"Resource not found: {uri}");
                        continue;
                    }

                    using var reader = new StreamReader(streamInfo.Stream, Encoding.UTF8);
                    var content = reader.ReadToEnd();
                    AddLicenseBlock(dir, content);
                }

                if (LicenseStackPanel.Children.Count == 0)
                {
                    LicenseStackPanel.Children.Add(new TextBlock { Text = "ライセンス情報が見つかりませんでした。", Margin = new Thickness(8) });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load license text: {ex}");
                LicenseStackPanel.Children.Add(new TextBlock { Text = "ライセンス情報の読み込みに失敗しました。\n\n" + ex.Message, Margin = new Thickness(8) });
            }
        }

        private void AddLicenseBlock(string title, string content)
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
            LicenseStackPanel.Children.Add(group);
        }
    }
}
