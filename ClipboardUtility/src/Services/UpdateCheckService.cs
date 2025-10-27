using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
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
    /// 現在のアプリケーションバージョンを取得します
    /// </summary>
    public static Version GetCurrentVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
 var version = assembly.GetName().Version;
            return version ?? new Version(0, 0, 0, 0);
        }
        catch (Exception ex)
   {
       Debug.WriteLine($"UpdateCheckService.GetCurrentVersion: Failed to get version: {ex}");
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

          // タグ名から "v" プレフィックスを削除してバージョンをパース
   var versionString = release.TagName.TrimStart('v', 'V');
       if (!Version.TryParse(versionString, out var latestVersion))
            {
          Debug.WriteLine($"UpdateCheckService: Failed to parse version from tag: {release.TagName}");
 return null;
            }

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
     public string TagName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string HtmlUrl { get; set; } = string.Empty;
     public DateTime PublishedAt { get; set; }
        public bool Draft { get; set; }
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
