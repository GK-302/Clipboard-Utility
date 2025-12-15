using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel; // 追加: MSIXパッケージ情報へのアクセス用
using ClipboardUtility.src.Helpers;

namespace ClipboardUtility.src.Services;

/// <summary>
/// アプリケーションの更新をチェックするサービス
/// GitHub Releases API を使用して最新バージョンを確認します
/// </summary>
public class UpdateCheckService
{
    private const string GITHUB_REPO_OWNER = "GK-302";
    private const string GITHUB_REPO_NAME = "Clipboard-Utility";
    private const string GITHUB_API_RELEASES_URL = $"https://api.github.com/repos/{GITHUB_REPO_OWNER}/{GITHUB_REPO_NAME}/releases/latest";

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static UpdateCheckService()
    {
        // GitHub API requires User-Agent header
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{GITHUB_REPO_NAME}/1.0");
    }

    /// <summary>
    /// 現在のアプリケーションバージョンを取得します。
    /// MSIX環境と非パッケージ環境（デバッグ）の両方に対応しています。
    /// </summary>
    public static Version GetCurrentVersion()
    {
        try
        {
            // 1. MSIXパッケージとして実行されているか試みる
            var package = Package.Current;
            var v = package.Id.Version;
            // MSIXのバージョン(PackageVersion構造体)をSystem.Versionに変換
            return new Version(v.Major, v.Minor, v.Build, v.Revision);
        }
        catch (InvalidOperationException)
        {
            // 2. パッケージ化されていない場合（Visual Studioでのデバッグ実行など）
            // アセンブリ情報からバージョンを取得するフォールバック処理
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version ?? new Version(0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateCheckService.GetCurrentVersion: Fallback failed: {ex}");
                return new Version(0, 0, 0, 0);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UpdateCheckService.GetCurrentVersion: Unexpected error: {ex}");
            FileLogger.LogException(ex, "UpdateCheckService.GetCurrentVersion");
            return new Version(0, 0, 0, 0);
        }
    }

    /// <summary>
    /// GitHub から最新のリリース情報を取得します
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            Debug.WriteLine($"UpdateCheckService: Checking for updates from {GITHUB_API_RELEASES_URL}");

            var response = await _httpClient.GetAsync(GITHUB_API_RELEASES_URL);

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"UpdateCheckService: GitHub API returned status code: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                Debug.WriteLine("UpdateCheckService: Failed to parse release information");
                return null;
            }

            // タグ名から "v" プレフィックスを削除
            var versionString = release.TagName.TrimStart('v', 'V');

            // プレリリース版（"-"を含む）は除外する
            if (versionString.Contains('-'))
            {
                Debug.WriteLine($"UpdateCheckService: Skipping pre-release version: {release.TagName}");
                return null;
            }

            // GitHubのバージョンをパース
            if (!Version.TryParse(versionString, out var parsedLatestVersion))
            {
                Debug.WriteLine($"UpdateCheckService: Failed to parse version from tag: {release.TagName}");
                return null;
            }

            // 【重要】比較用に4桁に正規化する
            // GitHubタグが "1.2.3" (Revision = -1) で、現在地が "1.2.3.0" (Revision = 0) の場合、
            // そのままだと Current > Latest と判定されたり、正しく比較できない場合があるため揃えます。
            var latestVersion = new Version(
                parsedLatestVersion.Major,
                parsedLatestVersion.Minor,
                parsedLatestVersion.Build >= 0 ? parsedLatestVersion.Build : 0,
                parsedLatestVersion.Revision >= 0 ? parsedLatestVersion.Revision : 0
            );

            var currentVersion = GetCurrentVersion();
            var isUpdateAvailable = latestVersion > currentVersion;

            Debug.WriteLine($"UpdateCheckService: Current version: {currentVersion}, Latest version: {latestVersion}, Update available: {isUpdateAvailable}");

            return new UpdateInfo
            {
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                IsUpdateAvailable = isUpdateAvailable,
                ReleaseUrl = release.HtmlUrl ?? $"https://github.com/{GITHUB_REPO_OWNER}/{GITHUB_REPO_NAME}/releases",
                ReleaseNotes = release.Body ?? string.Empty,
                PublishedAt = release.PublishedAt
            };
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"UpdateCheckService: Network error: {ex.Message}");
            FileLogger.LogException(ex, "UpdateCheckService.CheckForUpdatesAsync: Network error");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"UpdateCheckService: Request timeout: {ex.Message}");
            FileLogger.LogException(ex, "UpdateCheckService.CheckForUpdatesAsync: Timeout");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UpdateCheckService: Unexpected error: {ex.Message}");
            FileLogger.LogException(ex, "UpdateCheckService.CheckForUpdatesAsync: Unexpected error");
            return null;
        }
    }

    /// <summary>
    /// ブラウザで更新ページを開きます
    /// </summary>
    public void OpenReleasePage(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
            Debug.WriteLine($"UpdateCheckService: Opened release page: {url}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UpdateCheckService: Failed to open release page: {ex.Message}");
            FileLogger.LogException(ex, "UpdateCheckService.OpenReleasePage");
        }
    }

    #region GitHub API Response Models

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }
    }

    #endregion
}

/// <summary>
/// 更新情報を格納するクラス
/// </summary>
public class UpdateInfo
{
    public Version CurrentVersion { get; set; } = new Version(0, 0, 0, 0);
    public Version LatestVersion { get; set; } = new Version(0, 0, 0, 0);
    public bool IsUpdateAvailable { get; set; }
    public string ReleaseUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}