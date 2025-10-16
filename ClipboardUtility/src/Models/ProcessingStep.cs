using ClipboardUtility.src.Services;
using System.Text.Json.Serialization;

namespace ClipboardUtility.src.Models
{
    /// <summary>
    /// �v���Z�b�g���̒P�ꏈ���X�e�b�v��\���܂��B
    /// </summary>
    public sealed class ProcessingStep
    {
        /// <summary>
        /// ���s�����i0����n�܂�j
        /// </summary>
        [JsonPropertyName("order")]
        public int Order { get; set; }

        /// <summary>
        /// �������[�h
        /// </summary>
        [JsonPropertyName("mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcessingMode Mode { get; set; }

        /// <summary>
        /// ���̃X�e�b�v���L�����ǂ���
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// �I�v�V�����ݒ�iJSON ���畜������ۂɎg�p�j
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
        /// �f�B�[�v�R�s�[���쐬���܂��B
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
    /// ProcessingStep �ŗL�̃I�v�V�����iTextProcessingOptions �̃V���A���C�Y�\�Łj
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