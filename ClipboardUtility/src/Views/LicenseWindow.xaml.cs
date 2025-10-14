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
                // OFL.txt files are now set as "Content" build action, so they are copied to output directory
                var baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
                var foundFiles = new List<string>();

                try
                {
                    if (Directory.Exists(baseDir))
                    {
                        // Search for files named OFL.txt under the output directory
                        foundFiles = Directory.EnumerateFiles(baseDir, "OFL.txt", SearchOption.AllDirectories).ToList();
                        Debug.WriteLine($"Searching for OFL.txt in: {baseDir}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while searching for license files on disk: {ex}");
                }

                if (foundFiles.Count > 0)
                {
                    Debug.WriteLine($"Found {foundFiles.Count} license file(s) on disk.");
                    foreach (var file in foundFiles)
                    {
                        Debug.WriteLine($"Loading license from: {file}");
                        
                        // Extract the font directory name (e.g., "Roboto" or "NotoSansJP")
                        string title = Path.GetFileName(Path.GetDirectoryName(file)) ?? Path.GetFileName(file);
                        
                        string content = File.ReadAllText(file, Encoding.UTF8);
                        AddLicenseBlock(title, content);
                    }
                }
                else
                {
                    Debug.WriteLine("No OFL.txt files found in the output directory.");
                    LicenseStackPanel.Children.Add(new TextBlock 
                    { 
                        Text = "ライセンス情報が見つかりませんでした。\n\n" +
                               $"検索パス: {baseDir}", 
                        Margin = new Thickness(8),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load license text: {ex}");
                LicenseStackPanel.Children.Add(new TextBlock 
                { 
                    Text = "ライセンス情報の読み込みに失敗しました。\n\n" + ex.Message, 
                    Margin = new Thickness(8),
                    TextWrapping = TextWrapping.Wrap
                });
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
