using ClipboardUtility.src.Models;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace ClipboardUtility.src.Services;

/// <summary>
/// プリセットの管理と実行を担当するサービス。
/// </summary>
internal class PresetService
{
    private readonly TextProcessingService _textProcessingService;
    private readonly string _appDataDirectory;
    private readonly string _appDataPresetPath;
    private readonly string _projectPresetPath;
    private readonly string _presetFilePath;
    private readonly string _builtInPresetFilePath;
    private List<ProcessingPreset> _presets = [];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PresetService(TextProcessingService textProcessingService)
    {
        _textProcessingService = textProcessingService ?? throw new ArgumentNullException(nameof(textProcessingService));

        var productFolder = GetProductFolderName();
        _appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), productFolder, "config");
        _appDataPresetPath = Path.Combine(_appDataDirectory, "presets.json");
        _projectPresetPath = Path.Combine(AppContext.BaseDirectory, "config", "presets.json");
        _presetFilePath = _appDataPresetPath;
        _builtInPresetFilePath = _projectPresetPath;
    }

    /// <summary>
    /// 現在読み込まれているすべてのプリセット
    /// </summary>
    public IReadOnlyList<ProcessingPreset> Presets => _presets.AsReadOnly();

    /// <summary>
    /// ビルトインプリセットのみを取得
    /// </summary>
    public IEnumerable<ProcessingPreset> GetBuiltInPresets() => _presets.Where(p => p.IsBuiltIn);

    /// <summary>
    /// ユーザー作成プリセットのみを取得
    /// </summary>
    public IEnumerable<ProcessingPreset> GetUserPresets() => _presets.Where(p => !p.IsBuiltIn);

    /// <summary>
    /// プリセットを読み込みます（ビルトイン + ユーザー作成）
    /// </summary>
    public void LoadPresets()
    {
        _presets.Clear();

        Debug.WriteLine($"PresetService.LoadPresets: start. projectPresetExists={File.Exists(_projectPresetPath)}, appDataUserPresetExists={File.Exists(_appDataPresetPath)}");

        // 1) ビルトイン（配布）を常に読み込む（読み取り専用）
        //Debug.WriteLine($"PresetService.LoadPresets: Loading built-in presets from '{_projectPresetPath}'");
        var builtInPresets = LoadPresetsFromFile(_projectPresetPath, isBuiltIn: true) ?? new List<ProcessingPreset>();
        //Debug.WriteLine($"PresetService.LoadPresets: Loaded {builtInPresets.Count} built-in presets from project file.");
        //LogPresetList("built-in (from project)", builtInPresets);
        _presets.AddRange(builtInPresets);
        //Debug.WriteLine($"PresetService.LoadPresets: _presets.Count after adding built-ins = {_presets.Count}");

        // 2) ユーザープリセット（AppData）を読み込み（存在しなければ空で作成）
        if (!File.Exists(_appDataPresetPath))
        {
            //Debug.WriteLine($"PresetService.LoadPresets: No user preset file in AppData; creating empty user preset at '{_appDataPresetPath}'");
            try
            {
                Directory.CreateDirectory(_appDataDirectory);
                var empty = new { version = "1.0", presets = new List<ProcessingPreset>() };
                var jsonString = JsonSerializer.Serialize(empty, _jsonOptions);
                File.WriteAllText(_appDataPresetPath, jsonString, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PresetService.LoadPresets: Failed to create empty user preset file: {ex}");
            }
        }

        if (File.Exists(_appDataPresetPath))
        {
            //Debug.WriteLine($"PresetService.LoadPresets: Loading user presets from '{_appDataPresetPath}'");
            var userPresets = LoadPresetsFromFile(_appDataPresetPath, isBuiltIn: false) ?? new List<ProcessingPreset>();
            //Debug.WriteLine($"PresetService.LoadPresets: Loaded {userPresets.Count} user presets from AppData.");
            //LogPresetList("user (from AppData)", userPresets);
            _presets.AddRange(userPresets);
            //Debug.WriteLine($"PresetService.LoadPresets: _presets.Count after adding users = {_presets.Count}");
        }

        // 3) ローカライズ
        //Debug.WriteLine($"PresetService.LoadPresets: Starting localization. total presets = {_presets.Count}");
        LocalizeBuiltInPresets();
        //Debug.WriteLine($"PresetService.LoadPresets: Finished loading presets. final total = {_presets.Count}");
    }

    /// <summary>
    /// ユーザープリセットを保存します（ビルトインは保存しない）
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
        Directory.CreateDirectory(_appDataDirectory);
        File.WriteAllText(_appDataPresetPath, jsonString, Encoding.UTF8);
        Debug.WriteLine($"PresetService.SaveUserPresets: Saved {userPresets.Count} user presets to '{_appDataPresetPath}' (bytes={Encoding.UTF8.GetByteCount(jsonString)})");
    }

    /// <summary>
    /// 新しいプリセットを作成して追加します
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

        Debug.WriteLine($"PresetService: Creating new preset: Name={name}, Id={preset.Id}, Steps={steps?.Count ?? 0}");
        _presets.Add(preset);
        SaveUserPresets();
        Debug.WriteLine($"PresetService: Created and saved preset {preset.Id}");
        return preset;
    }

    /// <summary>
    /// プリセットを更新します（ビルトインは更新不可）
    /// </summary>
    public bool UpdatePreset(ProcessingPreset preset)
    {
        if (preset.IsBuiltIn) return false;

        var existing = _presets.FirstOrDefault(p => p.Id == preset.Id);
        if (existing == null) return false;

        Debug.WriteLine($"PresetService: Updating preset {preset.Id}");
        existing.Name = preset.Name;
        existing.Description = preset.Description;
        existing.Steps = preset.Steps;
        existing.ModifiedAt = DateTime.UtcNow;

        SaveUserPresets();
        Debug.WriteLine($"PresetService: Updated preset {preset.Id}");
        return true;
    }

    /// <summary>
    /// プリセットを削除します（ビルトインは削除不可）
    /// </summary>
    public bool DeletePreset(Guid id)
    {
        var preset = _presets.FirstOrDefault(p => p.Id == id);
        if (preset == null || preset.IsBuiltIn) return false;

        Debug.WriteLine($"PresetService: Deleting preset {id}");
        _presets.Remove(preset);
        SaveUserPresets();
        Debug.WriteLine($"PresetService: Deleted preset {id}");
        return true;
    }

    /// <summary>
    /// ID でプリセットを取得します
    /// </summary>
    public ProcessingPreset? GetPresetById(Guid id) => _presets.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// プリセットを実行します
    /// </summary>
    public string ExecutePreset(ProcessingPreset preset, string? input)
    {
        if (preset == null) throw new ArgumentNullException(nameof(preset));
        if (string.IsNullOrEmpty(input)) return string.Empty;

        Debug.WriteLine($"PresetService: Executing preset {preset.Id} on input length {input.Length}");
        var result = input;
        foreach (var step in preset.GetEnabledSteps())
        {
            var options = ConvertStepOptionsToTextProcessingOptions(step.Options);
                Debug.WriteLine($"PresetService: Executing step Order={step.Order}, Mode={step.Mode}, IsEnabled={step.IsEnabled}");
            result = _textProcessingService.Process(result, step.Mode, options);
        }

        Debug.WriteLine($"PresetService: Execution complete. Result length {result?.Length ?? 0}");
        return result;
    }

    /// <summary>
    /// プリセットを ID で実行します
    /// </summary>
    public string? ExecutePresetById(Guid id, string? input)
    {
        var preset = GetPresetById(id);
        return preset != null ? ExecutePreset(preset, input) : null;
    }

    // --- Private Helper Methods ---

    private List<ProcessingPreset> LoadPresetsFromFile(string filePath, bool isBuiltIn)
    {
        if (!File.Exists(filePath)) {
            //Debug.WriteLine($"PresetService.LoadPresetsFromFile: Preset file '{filePath}' does not exist.");
            return [];
        };

        try
        {
            //Debug.WriteLine($"PresetService.LoadPresetsFromFile: Reading presets file '{filePath}' (isBuiltIn={isBuiltIn})");
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("presets", out var presetsElement))
            {
                var presets = JsonSerializer.Deserialize<List<ProcessingPreset>>(presetsElement.GetRawText(), _jsonOptions) ?? [];
                
                // IsBuiltIn フラグを強制設定
                foreach (var preset in presets)
                {
                    preset.IsBuiltIn = isBuiltIn;
                }

                //Debug.WriteLine($"PresetService.LoadPresetsFromFile: Loaded {presets.Count} presets from '{filePath}' (isBuiltIn={isBuiltIn})");
                // 詳細ログ（ID/Name/ResourceKey）を出す
                for (int i = 0; i < presets.Count; i++)
                {
                    var p = presets[i];
                    //Debug.WriteLine($"  [{i}] {(isBuiltIn ? "BUILTIN" : "USER")} Id={p.Id}, Name='{p.Name}', NameResourceKey='{p.NameResourceKey}', IsBuiltIn={p.IsBuiltIn}");
                }

                return presets;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PresetService.LoadPresetsFromFile: Failed to load presets from '{filePath}': {ex}");
        }

        return [];
    }

    private void LocalizeBuiltInPresets()
    {
        foreach (var preset in GetBuiltInPresets())
        {
            //Debug.WriteLine($"PresetService: Localizing built-in preset Id={preset.Id}, ResourceName={preset.NameResourceKey}");
            if (!string.IsNullOrEmpty(preset.NameResourceKey))
            {
                var res = GetResourceString(preset.NameResourceKey);
                //Debug.WriteLine($"PresetService: Resource lookup for {preset.NameResourceKey} => {(res ?? "(null)")} ");
                preset.Name = res ?? preset.Name;
            }

            if (!string.IsNullOrEmpty(preset.DescriptionResourceKey))
            {
                var res = GetResourceString(preset.DescriptionResourceKey);
                //Debug.WriteLine($"PresetService: Resource lookup for {preset.DescriptionResourceKey} => {(res ?? "(null)")} ");
                preset.Description = res ?? preset.Description;
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

    // 補助: 製品名取得
    private static string GetProductFolderName()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return "ClipboardUtility";
            var productAttribute = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            return productAttribute?.Product ?? entryAssembly.GetName().Name ?? "ClipboardUtility";
        }
        catch
        {
            return "ClipboardUtility";
        }
    }

    // デバッグ用：プリセット一覧を簡潔にログ出力するヘルパー
    private void LogPresetList(string tag, IEnumerable<ProcessingPreset> presets)
    {
        var list = presets?.ToList() ?? new List<ProcessingPreset>();
        Debug.WriteLine($"PresetService.LogPresetList: [{tag}] count={list.Count}");
        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            Debug.WriteLine($"  [{i}] Id={p.Id} Name='{p.Name}' IsBuiltIn={p.IsBuiltIn} NameResourceKey='{p.NameResourceKey}'");
        }
    }
}