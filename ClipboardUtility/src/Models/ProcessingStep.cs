using ClipboardUtility.src.Services;
using System.Text.Json.Serialization;

namespace ClipboardUtility.src.Models
{
    /// <summary>
    /// プリセット内の単一処理ステップを表します。
    /// </summary>
    public sealed class ProcessingStep
    {
        /// <summary>
        /// 実行順序（0から始まる）
        /// </summary>
        [JsonPropertyName("order")]
        public int Order { get; set; }

        /// <summary>
        /// 処理モード
        /// </summary>
        [JsonPropertyName("mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcessingMode Mode { get; set; }

        /// <summary>
        /// このステップが有効かどうか
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// オプション設定（JSON から復元する際に使用）
        /// </summary>
        [JsonPropertyName("options")]
        public ProcessingStepOptions? Options { get; set; }

        public ProcessingStep()
        {
        }

        public ProcessingStep(int order, ProcessingMode mode, bool isEnabled = true)
        {
            Order = order;
            Mode = mode;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// ディープコピーを作成します。
        /// </summary>
        public ProcessingStep Clone()
        {
            return new ProcessingStep
            {
                Order = Order,
                Mode = Mode,
                IsEnabled = IsEnabled,
                Options = Options?.Clone()
            };
        }
    }

    /// <summary>
    /// ProcessingStep 固有のオプション（TextProcessingOptions のシリアライズ可能版）
    /// </summary>
    public sealed class ProcessingStepOptions
    {
        [JsonPropertyName("tabSize")]
        public int? TabSize { get; set; }

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("truncateSuffix")]
        public string? TruncateSuffix { get; set; }

        [JsonPropertyName("normalizationForm")]
        public string? NormalizationForm { get; set; }

        [JsonPropertyName("cultureName")]
        public string? CultureName { get; set; }

        public ProcessingStepOptions Clone()
        {
            return new ProcessingStepOptions
            {
                TabSize = TabSize,
                MaxLength = MaxLength,
                TruncateSuffix = TruncateSuffix,
                NormalizationForm = NormalizationForm,
                CultureName = CultureName
            };
        }
    }
}