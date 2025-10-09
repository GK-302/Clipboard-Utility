using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ClipboardUtility.src.Helpers
{
    /// <summary>
    /// シンプルなファイルロガー。スレッドセーフにログを追記します。
    /// ログファイルは %LOCALAPPDATA%\ClipboardUtility\logs\app.log に作成されます。
    /// </summary>
    internal static class FileLogger
    {
        private static readonly object _fileLock = new object();
        private static readonly string _logDirectory;
        private static readonly string _logFilePath;

        static FileLogger()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logDirectory = Path.Combine(localAppData, "ClipboardUtility", "logs");
                Directory.CreateDirectory(_logDirectory);
                _logFilePath = Path.Combine(_logDirectory, "app.log");
            }
            catch
            {
                // 初期化失敗時はフォールバックとして実行ディレクトリを使用
                _logDirectory = AppContext.BaseDirectory ?? ".";
                _logFilePath = Path.Combine(_logDirectory, "app.log");
            }
        }

        public static void Log(string message)
        {
            try
            {
                var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var line = $"{ts} [T{threadId}] {message}" + Environment.NewLine;

                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // ログ記録失敗は無視（アプリ本体の動作を妨げない）
            }
        }

        public static void LogException(Exception ex, string? context = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(context ?? "Exception");
                sb.AppendLine(ex.GetType().FullName + ": " + ex.Message);
                sb.AppendLine(ex.StackTrace ?? string.Empty);

                if (ex.InnerException != null)
                {
                    sb.AppendLine("--- Inner Exception ---");
                    sb.AppendLine(ex.InnerException.GetType().FullName + ": " + ex.InnerException.Message);
                    sb.AppendLine(ex.InnerException.StackTrace ?? string.Empty);
                }

                Log(sb.ToString());
            }
            catch
            {
                // 無視
            }
        }

        public static string GetLogFilePath() => _logFilePath;
    }
}
