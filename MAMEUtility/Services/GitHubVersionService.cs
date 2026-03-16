using System.Net.Http;
using System.Text.Json;
using MAMEUtility.Interfaces;

namespace MAMEUtility.Services;

public class GitHubVersionService : IVersionCheckService, IDisposable
{
    private const string RepoUrl = "https://api.github.com/repos/drpetersonfernandes/MAMEUtility/releases/latest";
    private readonly HttpClient _httpClient = new();

    public GitHubVersionService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MAMEUtility-App");
    }

    public async Task<(bool IsNewVersionAvailable, string? LatestVersion, string? DownloadUrl)> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(RepoUrl);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.TryGetProperty("tag_name", out var tagElement))
            {
                var latestVersionTag = tagElement.GetString() ?? string.Empty;
                var latestVersionStr = latestVersionTag.TrimStart('v');
                var currentVersionStr = AboutWindow.RawVersion;

                if (Version.TryParse(latestVersionStr, out var latestVersion) &&
                    Version.TryParse(currentVersionStr, out var currentVersion))
                {
                    if (latestVersion > currentVersion)
                    {
                        var htmlUrl = root.GetProperty("html_url").GetString();
                        return (true, latestVersionTag, htmlUrl);
                    }
                }
            }

            return (false, null, null);
        }
        catch
        {
            return (false, null, null);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
