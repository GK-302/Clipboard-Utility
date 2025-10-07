using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ClipboardUtility.src.Services
{
    /// <summary>
    /// 文字列処理を集約するサービス。
    /// 多くの処理モードを提供し、将来の拡張・テスト・設定による切替えがしやすい形にしています。
    /// </summary>
    internal class TextProcessingService
    {
        // コンパイル済み正規表現をキャッシュしてパフォーマンスを確保
        private static readonly Regex _multiWhitespaceRegex = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _urlRegex = new(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex _emailRegex = new(@"\b[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex _htmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex _markdownLinkRegex = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _punctuationRegex = new(@"\p{P}+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _controlCharsRegex = new(@"[\p{Cc}]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// 文字数カウント（ヌル安全）
        /// </summary>
        public int CountCharacters(string? input) => string.IsNullOrEmpty(input) ? 0 : input!.Length;

        /// <summary>
        /// 改行をスペースに置換します（元の挙動を保持）。
        /// </summary>
        public string RemoveLineBreaks(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input!.Replace("\r\n", " ").Replace("\n", " ");
        }

        /// <summary>
        /// 改行・タブをスペースに変換し、連続する空白を単一スペースに正規化し、前後を trim します。
        /// </summary>
        public string NormalizeWhitespace(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var replaced = input!.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ");
            replaced = _multiWhitespaceRegex.Replace(replaced, " ").Trim();
            return replaced;
        }

        /// <summary>
        /// 指定サイズでタブをスペースに変換します（デフォルト tabSize = 4）。
        /// </summary>
        public string ConvertTabsToSpaces(string? input, int tabSize = 4)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input!.Replace("\t", new string(' ', Math.Max(1, tabSize)));
        }

        /// <summary>
        /// 前後の空白を取り除きます。
        /// </summary>
        public string Trim(string? input) => string.IsNullOrEmpty(input) ? string.Empty : input!.Trim();

        /// <summary>
        /// 文字列を大文字にします（カルチャ依存）。
        /// </summary>
        public string ToUpper(string? input, CultureInfo? culture = null)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return (culture ?? CultureInfo.CurrentCulture).TextInfo.ToUpper(input!);
        }

        /// <summary>
        /// 文字列を小文字にします（カルチャ依存）。
        /// </summary>
        public string ToLower(string? input, CultureInfo? culture = null)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return (culture ?? CultureInfo.CurrentCulture).TextInfo.ToLower(input!);
        }

        /// <summary>
        /// パンクチュエーション（句読点）を削除します。
        /// </summary>
        public string RemovePunctuation(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return _punctuationRegex.Replace(input!, "");
        }

        /// <summary>
        /// 制御文字（コントロールコード）を削除します。
        /// </summary>
        public string RemoveControlCharacters(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return _controlCharsRegex.Replace(input!, "");
        }

        /// <summary>
        /// URL を削除します。
        /// </summary>
        public string RemoveUrls(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return _urlRegex.Replace(input!, "");
        }

        /// <summary>
        /// メールアドレスを削除します（簡易）。
        /// </summary>
        public string RemoveEmails(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return _emailRegex.Replace(input!, "");
        }

        /// <summary>
        /// HTML タグを除去します（簡易実装）。
        /// </summary>
        public string RemoveHtmlTags(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return _htmlTagRegex.Replace(input!, "");
        }

        /// <summary>
        /// Markdown の簡易リンク構文をテキスト（リンクテキスト）に置換します。
        /// [text](url) -> text
        /// </summary>
        public string StripMarkdownLinks(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return _markdownLinkRegex.Replace(input!, "$1");
        }

        /// <summary>
        /// Unicode 正規化（既定: FormKC）を行います。
        /// </summary>
        public string NormalizeUnicode(string? input, NormalizationForm form = NormalizationForm.FormKC)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input!.Normalize(form);
        }

        /// <summary>
        /// ダイアクリティカルマーク（合字やアクセント）を除去して ASCII に近い文字列を作る（完全ではない）。
        /// </summary>
        public string RemoveDiacritics(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var normalized = input!.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// 先頭から指定長まで切り詰め、必要ならサフィックスを追加します。
        /// </summary>
        public string Truncate(string? input, int maxLength, string suffix = "…")
        {
            if (string.IsNullOrEmpty(input) || maxLength <= 0) return string.Empty;
            if (input!.Length <= maxLength) return input;
            var truncated = input.Substring(0, Math.Max(0, maxLength - suffix.Length));
            return truncated + suffix;
        }

        /// <summary>
        /// 行を結合して単一のスペースでつなぎます（改行をスペース化）。
        /// </summary>
        public string JoinLinesWithSpace(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var lines = input!.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", lines).Trim();
        }

        /// <summary>
        /// 重複する連続行（隣接する同一行）を削除します。
        /// </summary>
        public string RemoveDuplicateLines(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var lines = input!.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            string? prev = null;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != prev)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(lines[i]);
                }
                prev = lines[i];
            }
            return sb.ToString();
        }

        /// <summary>
        /// キャメルケースまたはパスカルケースに変換します（単純実装: 非英数字で分割）。
        /// </summary>
        public string ToPascalCase(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var parts = _multiWhitespaceRegex.Replace(input!, " ").Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                var s = p.ToLowerInvariant();
                sb.Append(char.ToUpperInvariant(s[0]));
                if (s.Length > 1) sb.Append(s.Substring(1));
            }
            return sb.ToString();
        }

        public string ToCamelCase(string? input)
        {
            var pascal = ToPascalCase(input);
            if (string.IsNullOrEmpty(pascal)) return string.Empty;
            return char.ToLowerInvariant(pascal[0]) + (pascal.Length > 1 ? pascal.Substring(1) : string.Empty);
        }

        /// <summary>
        /// 文字列をタイトルケースにします（カルチャ依存）。
        /// </summary>
        public string ToTitleCase(string? input, CultureInfo? culture = null)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var info = (culture ?? CultureInfo.CurrentCulture).TextInfo;
            return info.ToTitleCase(input!.ToLower(culture ?? CultureInfo.CurrentCulture));
        }

        // --- 処理モードに応じて呼び分ける統合インターフェイス ---

        /// <summary>
        /// シンプルな Process（追加オプション不要の場合に使用）
        /// </summary>
        public string Process(string? input, ProcessingMode mode) => Process(input, mode, options: null);

        /// <summary>
        /// 処理モードとオプションに従ってテキストを加工します。
        /// </summary>
        public string Process(string? input, ProcessingMode mode, TextProcessingOptions? options)
        {
            options ??= TextProcessingOptions.Default;

            return mode switch
            {
                ProcessingMode.None => input ?? string.Empty,
                ProcessingMode.RemoveLineBreaks => RemoveLineBreaks(input),
                ProcessingMode.NormalizeWhitespace => NormalizeWhitespace(input),
                ProcessingMode.NormalizeUnicode => NormalizeUnicode(input, options.NormalizationForm),
                ProcessingMode.RemoveDiacritics => RemoveDiacritics(input),
                ProcessingMode.RemovePunctuation => RemovePunctuation(input),
                ProcessingMode.RemoveControlCharacters => RemoveControlCharacters(input),
                ProcessingMode.RemoveUrls => RemoveUrls(input),
                ProcessingMode.RemoveEmails => RemoveEmails(input),
                ProcessingMode.RemoveHtmlTags => RemoveHtmlTags(input),
                ProcessingMode.StripMarkdownLinks => StripMarkdownLinks(input),
                ProcessingMode.ConvertTabsToSpaces => ConvertTabsToSpaces(input, options.TabSize),
                ProcessingMode.Trim => Trim(input),
                ProcessingMode.ToUpper => ToUpper(input, options.Culture),
                ProcessingMode.ToLower => ToLower(input, options.Culture),
                ProcessingMode.ToTitleCase => ToTitleCase(input, options.Culture),
                ProcessingMode.ToPascalCase => ToPascalCase(input),
                ProcessingMode.ToCamelCase => ToCamelCase(input),
                ProcessingMode.Truncate => Truncate(input, options.MaxLength, options.TruncateSuffix),
                ProcessingMode.JoinLinesWithSpace => JoinLinesWithSpace(input),
                ProcessingMode.RemoveDuplicateLines => RemoveDuplicateLines(input),
                ProcessingMode.CollapseWhitespace => _multiWhitespaceRegex.Replace(input ?? string.Empty, " ").Trim(),
                _ => input ?? string.Empty,
            };
        }
    }

    /// <summary>
    /// クリップボード処理のモード
    /// 必要に応じてモードを追加していく
    /// 各値に ResourceKey 属性を付与して Resources と結びつけられます。
    /// </summary>
    internal enum ProcessingMode
    {
        None = 0,

        [ResourceKey("NotificationFormat_LineBreakRemoved")]
        RemoveLineBreaks = 1,

        [ResourceKey("NotificationFormat_WhitespaceNormalized")]
        NormalizeWhitespace = 2,

        [ResourceKey("NotificationFormat_NormalizeUnicode")]
        NormalizeUnicode = 3,

        [ResourceKey("NotificationFormat_RemoveDiacritics")]
        RemoveDiacritics = 4,

        [ResourceKey("NotificationFormat_RemovePunctuation")]
        RemovePunctuation = 5,

        [ResourceKey("NotificationFormat_RemoveControlChars")]
        RemoveControlCharacters = 6,

        [ResourceKey("NotificationFormat_RemoveUrls")]
        RemoveUrls = 7,

        [ResourceKey("NotificationFormat_RemoveEmails")]
        RemoveEmails = 8,

        [ResourceKey("NotificationFormat_RemoveHtmlTags")]
        RemoveHtmlTags = 9,

        [ResourceKey("NotificationFormat_StripMarkdownLinks")]
        StripMarkdownLinks = 10,

        [ResourceKey("NotificationFormat_ConvertTabsToSpaces")]
        ConvertTabsToSpaces = 11,

        [ResourceKey("NotificationFormat_Trim")]
        Trim = 12,

        [ResourceKey("NotificationFormat_ToUpper")]
        ToUpper = 13,

        [ResourceKey("NotificationFormat_ToLower")]
        ToLower = 14,

        [ResourceKey("NotificationFormat_ToTitleCase")]
        ToTitleCase = 15,

        [ResourceKey("NotificationFormat_ToPascalCase")]
        ToPascalCase = 16,

        [ResourceKey("NotificationFormat_ToCamelCase")]
        ToCamelCase = 17,

        [ResourceKey("NotificationFormat_Truncate")]
        Truncate = 18,

        [ResourceKey("NotificationFormat_JoinLinesWithSpace")]
        JoinLinesWithSpace = 19,

        [ResourceKey("NotificationFormat_RemoveDuplicateLines")]
        RemoveDuplicateLines = 20,

        [ResourceKey("NotificationFormat_CollapseWhitespace")]
        CollapseWhitespace = 21
    }

    /// <summary>
    /// Process に渡すオプション。将来設定画面や JSON から読み込めるように設計しています。
    /// </summary>
    internal sealed class TextProcessingOptions
    {
        public static TextProcessingOptions Default { get; } = new TextProcessingOptions();

        /// <summary>タブをスペースに変換する際の幅</summary>
        public int TabSize { get; init; } = 4;

        /// <summary>切り詰めの最大長（Truncate モード用）</summary>
        public int MaxLength { get; init; } = 200;

        /// <summary>切り詰め時のサフィックス</summary>
        public string TruncateSuffix { get; init; } = "…";

        /// <summary>Unicode 正規化フォーム</summary>
        public NormalizationForm NormalizationForm { get; init; } = NormalizationForm.FormKC;

        /// <summary>カルチャ（ToUpper/ToLower/ToTitleCase 用）</summary>
        public CultureInfo? Culture { get; init; } = CultureInfo.CurrentCulture;
    }
}
