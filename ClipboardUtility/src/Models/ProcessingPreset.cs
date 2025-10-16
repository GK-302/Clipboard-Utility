using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ClipboardUtility.src.Models
{
    /// <summary>
    /// 複数の処理ステップをまとめたプリセットを表します。
    /// </summary>
    public sealed class ProcessingPreset
    {
        /// <summary>
        /// プリセットの一意識別子
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// プリセット名
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// プリセットの説明
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 処理ステップのリスト（Order でソートされる）
        /// </summary>
        [JsonPropertyName("steps")]
        public List<ProcessingStep> Steps { get; set; } = new List<ProcessingStep>();

        /// <summary>
        /// 作成日時
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最終更新日時
        /// </summary>
        [JsonPropertyName("modifiedAt")]
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// ビルトインプリセットかどうか（ビルトインは削除・編集不可）
        /// </summary>
        [JsonPropertyName("isBuiltIn")]
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// 多言語対応用のリソースキー（ビルトインプリセット用）
        /// </summary>
        [JsonPropertyName("nameResourceKey")]
        public string? NameResourceKey { get; set; }

        /// <summary>
        /// 多言語対応用のリソースキー（ビルトインプリセット用）
        /// </summary>
        [JsonPropertyName("descriptionResourceKey")]
        public string? DescriptionResourceKey { get; set; }

        public ProcessingPreset()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// ディープコピーを作成します。
        /// </summary>
        public ProcessingPreset Clone()
        {
            return new ProcessingPreset
            {
                Id = Guid.NewGuid(), // 新しい ID を割り当て
                Name = Name,
                Description = Description,
                Steps = Steps.Select(s => s.Clone()).ToList(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                IsBuiltIn = false, // コピーはビルトインではない
                NameResourceKey = null,
                DescriptionResourceKey = null
            };
        }

        /// <summary>
        /// 有効なステップのみを Order 順に取得します。
        /// </summary>
        public IEnumerable<ProcessingStep> GetEnabledSteps()
        {
            return Steps.Where(s => s.IsEnabled).OrderBy(s => s.Order);
        }
    }
}