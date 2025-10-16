using ClipboardUtility.src.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipboardUtility.src.Services
{
    /// <summary>
    /// �v���Z�b�g�̊Ǘ��Ǝ��s��S������T�[�r�X�B
    /// </summary>
    internal class PresetManager
    {
        private readonly TextProcessingService _textProcessingService;
        private readonly string _presetFilePath;
        private readonly string _builtInPresetFilePath;
        private List<ProcessingPreset> _presets = [];

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        public PresetManager(TextProcessingService textProcessingService, string userPresetsPath = "config/user_presets.json", string builtInPresetsPath = "config/presets.json")
        {
            _textProcessingService = textProcessingService ?? throw new ArgumentNullException(nameof(textProcessingService));
            _presetFilePath = userPresetsPath;
            _builtInPresetFilePath = builtInPresetsPath;
        }

        /// <summary>
        /// ���ݓǂݍ��܂�Ă��邷�ׂẴv���Z�b�g
        /// </summary>
        public IReadOnlyList<ProcessingPreset> Presets => _presets.AsReadOnly();

        /// <summary>
        /// �r���g�C���v���Z�b�g�݂̂��擾
        /// </summary>
        public IEnumerable<ProcessingPreset> GetBuiltInPresets() => _presets.Where(p => p.IsBuiltIn);

        /// <summary>
        /// ���[�U�[�쐬�v���Z�b�g�݂̂��擾
        /// </summary>
        public IEnumerable<ProcessingPreset> GetUserPresets() => _presets.Where(p => !p.IsBuiltIn);

        /// <summary>
        /// �v���Z�b�g��ǂݍ��݂܂��i�r���g�C�� + ���[�U�[�쐬�j
        /// </summary>
        public void LoadPresets()
        {
            _presets.Clear();

            // 1. �r���g�C���v���Z�b�g��ǂݍ���
            var builtInPresets = LoadPresetsFromFile(_builtInPresetFilePath, isBuiltIn: true);
            _presets.AddRange(builtInPresets);

            // 2. ���[�U�[�v���Z�b�g��ǂݍ���
            if (File.Exists(_presetFilePath))
            {
                var userPresets = LoadPresetsFromFile(_presetFilePath, isBuiltIn: false);
                _presets.AddRange(userPresets);
            }

            // 3. �r���g�C���v���Z�b�g�̃��\�[�X�L�[����\������ǂݍ���
            LocalizeBuiltInPresets();
        }

        /// <summary>
        /// ���[�U�[�v���Z�b�g��ۑ����܂��i�r���g�C���͕ۑ����Ȃ��j
        /// </summary>
        public void SaveUserPresets()
        {
            var userPresets = GetUserPresets().ToList();
            var json = new
            {
                version = "1.0",
                presets = userPresets
            };

            var jsonString = JsonSerializer.Serialize(json, _jsonOptions);
            var directory = Path.GetDirectoryName(_presetFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(_presetFilePath, jsonString, Encoding.UTF8);
        }

        /// <summary>
        /// �V�����v���Z�b�g���쐬���Ēǉ����܂�
        /// </summary>
        public ProcessingPreset CreatePreset(string name, string description, List<ProcessingStep> steps)
        {
            var preset = new ProcessingPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Steps = steps,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                IsBuiltIn = false
            };

            _presets.Add(preset);
            SaveUserPresets();
            return preset;
        }

        /// <summary>
        /// �v���Z�b�g���X�V���܂��i�r���g�C���͍X�V�s�j
        /// </summary>
        public bool UpdatePreset(ProcessingPreset preset)
        {
            if (preset.IsBuiltIn) return false;

            var existing = _presets.FirstOrDefault(p => p.Id == preset.Id);
            if (existing == null) return false;

            existing.Name = preset.Name;
            existing.Description = preset.Description;
            existing.Steps = preset.Steps;
            existing.ModifiedAt = DateTime.UtcNow;

            SaveUserPresets();
            return true;
        }

        /// <summary>
        /// �v���Z�b�g���폜���܂��i�r���g�C���͍폜�s�j
        /// </summary>
        public bool DeletePreset(Guid id)
        {
            var preset = _presets.FirstOrDefault(p => p.Id == id);
            if (preset == null || preset.IsBuiltIn) return false;

            _presets.Remove(preset);
            SaveUserPresets();
            return true;
        }

        /// <summary>
        /// ID �Ńv���Z�b�g���擾���܂�
        /// </summary>
        public ProcessingPreset? GetPresetById(Guid id) => _presets.FirstOrDefault(p => p.Id == id);

        /// <summary>
        /// �v���Z�b�g�����s���܂�
        /// </summary>
        public string ExecutePreset(ProcessingPreset preset, string? input)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var result = input;
            foreach (var step in preset.GetEnabledSteps())
            {
                var options = ConvertStepOptionsToTextProcessingOptions(step.Options);
                result = _textProcessingService.Process(result, step.Mode, options);
            }

            return result;
        }

        /// <summary>
        /// �v���Z�b�g�� ID �Ŏ��s���܂�
        /// </summary>
        public string? ExecutePresetById(Guid id, string? input)
        {
            var preset = GetPresetById(id);
            return preset != null ? ExecutePreset(preset, input) : null;
        }

        // --- Private Helper Methods ---

        private List<ProcessingPreset> LoadPresetsFromFile(string filePath, bool isBuiltIn)
        {
            if (!File.Exists(filePath)) return [];

            try
            {
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("presets", out var presetsElement))
                {
                    var presets = JsonSerializer.Deserialize<List<ProcessingPreset>>(presetsElement.GetRawText(), _jsonOptions) ?? [];
                    
                    // IsBuiltIn �t���O�������ݒ�
                    foreach (var preset in presets)
                    {
                        preset.IsBuiltIn = isBuiltIn;
                    }

                    return presets;
                }
            }
            catch (Exception ex)
            {
                // ���O�o�́i���ۂ̎����ł� ILogger ���g�p�j
                System.Diagnostics.Debug.WriteLine($"Failed to load presets from {filePath}: {ex.Message}");
            }

            return [];
        }

        private void LocalizeBuiltInPresets()
        {
            foreach (var preset in GetBuiltInPresets())
            {
                if (!string.IsNullOrEmpty(preset.NameResourceKey))
                {
                    preset.Name = GetResourceString(preset.NameResourceKey) ?? preset.Name;
                }

                if (!string.IsNullOrEmpty(preset.DescriptionResourceKey))
                {
                    preset.Description = GetResourceString(preset.DescriptionResourceKey) ?? preset.Description;
                }
            }
        }

        private static string? GetResourceString(string resourceKey)
        {
            try
            {
                var resourceManager = Properties.Resources.ResourceManager;
                return resourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture);
            }
            catch
            {
                return null;
            }
        }

        private static TextProcessingOptions? ConvertStepOptionsToTextProcessingOptions(ProcessingStepOptions? stepOptions)
        {
            if (stepOptions == null) return null;

            return new TextProcessingOptions
            {
                TabSize = stepOptions.TabSize ?? TextProcessingOptions.Default.TabSize,
                MaxLength = stepOptions.MaxLength ?? TextProcessingOptions.Default.MaxLength,
                TruncateSuffix = stepOptions.TruncateSuffix ?? TextProcessingOptions.Default.TruncateSuffix,
                NormalizationForm = ParseNormalizationForm(stepOptions.NormalizationForm),
                Culture = ParseCulture(stepOptions.CultureName)
            };
        }

        private static NormalizationForm ParseNormalizationForm(string? formName)
        {
            return formName switch
            {
                "FormC" => NormalizationForm.FormC,
                "FormD" => NormalizationForm.FormD,
                "FormKC" => NormalizationForm.FormKC,
                "FormKD" => NormalizationForm.FormKD,
                _ => TextProcessingOptions.Default.NormalizationForm
            };
        }

        private static CultureInfo? ParseCulture(string? cultureName)
        {
            if (string.IsNullOrEmpty(cultureName)) return CultureInfo.CurrentCulture;

            try
            {
                return CultureInfo.GetCultureInfo(cultureName);
            }
            catch
            {
                return CultureInfo.CurrentCulture;
            }
        }
    }
}