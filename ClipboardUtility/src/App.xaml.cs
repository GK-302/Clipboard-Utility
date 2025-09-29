using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;

namespace ClipboardUtility
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // --- ここから追加 ---

            // テストしたい言語のカルチャ情報を設定します。
            // "ja-JP": 日本語
            // "en-US": 英語 (米国)
            // この行をコメントアウトしたり書き換えたりすることで、テストする言語を切り替えられます。
            //var culture = new CultureInfo("ja-JP");
            //var culture = new CultureInfo("en-US");

            //Thread.CurrentThread.CurrentCulture = culture;
            //Thread.CurrentThread.CurrentUICulture = culture;
        }
    }

}
