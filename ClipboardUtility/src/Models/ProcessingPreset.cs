using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ClipboardUtility.src.Models
{
    /// <summary>
    /// �����̏����X�e�b�v���܂Ƃ߂��v���Z�b�g��\���܂��B
    /// </summary>
    public sealed class ProcessingPreset
    {
        /// <summary>
        /// �v���Z�b�g�̈�ӎ��ʎq
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// �v���Z�b�g��
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// �v���Z�b�g�̐���
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// �����X�e�b�v�̃��X�g�iOrder �Ń\�[�g�����j
        /// </summary>
        [JsonPropertyName("steps")]
        public List<ProcessingStep> Steps { get; set; } = new List<ProcessingStep>();

        /// <summary>
        /// �쐬����
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// �ŏI�X�V����
        /// </summary>
        [JsonPropertyName("modifiedAt")]
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// �r���g�C���v���Z�b�g���ǂ����i�r���g�C���͍폜�E�ҏW�s�j
        /// </summary>
        [JsonPropertyName("isBuiltIn")]
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// ������Ή��p�̃��\�[�X�L�[�i�r���g�C���v���Z�b�g�p�j
        /// </summary>
        [JsonPropertyName("nameResourceKey")]
        public string? NameResourceKey { get; set; }

        /// <summary>
        /// ������Ή��p�̃��\�[�X�L�[�i�r���g�C���v���Z�b�g�p�j
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
        /// �f�B�[�v�R�s�[���쐬���܂��B
        /// </summary>
        public ProcessingPreset Clone()
        {
            return new ProcessingPreset
            {
                Id = Guid.NewGuid(), // �V���� ID �����蓖��
                Name = Name,
                Description = Description,
                Steps = Steps.Select(s => s.Clone()).ToList(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                IsBuiltIn = false, // �R�s�[�̓r���g�C���ł͂Ȃ�
                NameResourceKey = null,
                DescriptionResourceKey = null
            };
        }

        /// <summary>
        /// �L���ȃX�e�b�v�݂̂� Order ���Ɏ擾���܂��B
        /// </summary>
        public IEnumerable<ProcessingStep> GetEnabledSteps()
        {
            return Steps.Where(s => s.IsEnabled).OrderBy(s => s.Order);
        }
    }
}