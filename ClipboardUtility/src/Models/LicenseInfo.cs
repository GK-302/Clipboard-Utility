using System.Text.Json.Serialization;

namespace ClipboardUtility.src.Models
{
    /// <summary>
    /// Represents license information for a NuGet package.
    /// This model matches the JSON output format from dotnet-project-licenses tool.
    /// </summary>
    public class LicenseInfo
    {
        [JsonPropertyName("PackageName")]
        public string PackageName { get; set; } = string.Empty;

        [JsonPropertyName("PackageVersion")]
        public string PackageVersion { get; set; } = string.Empty;

        [JsonPropertyName("PackageUrl")]
        public string PackageUrl { get; set; } = string.Empty;

        [JsonPropertyName("Copyright")]
        public string Copyright { get; set; } = string.Empty;

        [JsonPropertyName("Authors")]
        public string[] Authors { get; set; } = [];

        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("LicenseUrl")]
        public string LicenseUrl { get; set; } = string.Empty;

        [JsonPropertyName("LicenseType")]
        public string LicenseType { get; set; } = string.Empty;

        /// <summary>
        /// Display name combining package name and version.
        /// </summary>
        [JsonIgnore]
        public string DisplayName => string.IsNullOrEmpty(PackageVersion) 
            ? PackageName 
            : $"{PackageName} v{PackageVersion}";

        /// <summary>
        /// Authors as a comma-separated string for display purposes.
        /// </summary>
        [JsonIgnore]
        public string AuthorsDisplay => Authors.Length > 0 ? string.Join(", ", Authors) : string.Empty;
    }
}
