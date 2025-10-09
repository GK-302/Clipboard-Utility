using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ClipboardUtility.src.Helpers
{
    /// <summary>
    /// �V���v���ȃt�@�C�����K�[�B�X���b�h�Z�[�t�Ƀ��O��ǋL���܂��B
    /// ���O�t�@�C���� %LOCALAPPDATA%\ClipboardUtility\logs\app.log �ɍ쐬����܂��B
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
                // ���������s���̓t�H�[���o�b�N�Ƃ��Ď��s�f�B���N�g�����g�p
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
                // ���O�L�^���s�͖����i�A�v���{�̂̓����W���Ȃ��j
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
                // ����
            }
        }

        public static string GetLogFilePath() => _logFilePath;
    }
}
