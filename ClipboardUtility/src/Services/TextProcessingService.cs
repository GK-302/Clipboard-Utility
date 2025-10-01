using System;
using System.Text.RegularExpressions;

namespace ClipboardUtility.src.Services
{
    /// <summary>
    /// 文字列処理を集約するサービス。
    /// 将来メソッドを追加して処理モードを増やしていけるように設計しています。
    /// </summary>
    internal class TextProcessingService
    {
        public int CountCharacters(string input)
        {
            return string.IsNullOrEmpty(input) ? 0 : input.Length;
        }

        /// <summary>
        /// 改行をスペースに置換します（元の挙動を保持）。
        /// </summary>
        public string RemoveLineBreaks(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Replace("\r\n", " ").Replace("\n", " ");
        }

        /// <summary>
        /// 改行・タブをスペースに変換し、連続する空白を単一スペースに正規化し、前後を trim します。
        /// より堅牢な正規化が必要な場合はここを拡張してください。
        /// </summary>
        public string NormalizeWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // 改行・タブをスペースに、連続空白を単一スペースに正規化して trim
            var replaced = input.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ");
            replaced = Regex.Replace(replaced, @"\s+", " ").Trim();
            return replaced;
        }

        /// <summary>
        /// 処理モードに応じてテキストを加工します。
        /// 新しいモードを追加する場合はここにケースを追加してください。
        /// </summary>
        public string Process(string input, ProcessingMode mode)
        {
            return mode switch
            {
                ProcessingMode.RemoveLineBreaks => RemoveLineBreaks(input),
                ProcessingMode.NormalizeWhitespace => NormalizeWhitespace(input),
                ProcessingMode.None or _ => input ?? string.Empty,
            };
        }
    }

    /// <summary>
    /// クリップボード処理のモード
    /// 必要に応じてモードを追加していく
    /// </summary>
    internal enum ProcessingMode
    {
        None = 0,

        [ResourceKey("NotificationFormat_LineBreakRemoved")]
        RemoveLineBreaks = 1,

        [ResourceKey("NotificationFormat_WhitespaceNormalized")]
        NormalizeWhitespace = 2
    }
}
