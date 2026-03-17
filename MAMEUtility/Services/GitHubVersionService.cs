using System.Net.Http;
using System.Text.Json;
using MAMEUtility.Interfaces;

namespace MAMEUtility.Services;

public class GitHubVersionService : IVersionCheckService, IDisposable
{
    private const string RepoUrl = "https://api.github.com/repos/drpetersonfernandes/MAMEUtility/releases/latest";
    private readonly HttpClient _httpClient;
    private readonly IVersionService _versionService;

    public GitHubVersionService(HttpClient httpClient, IVersionService versionService)
    {
        _httpClient = httpClient;
        _versionService = versionService;
    }

    public async Task<(bool IsNewVersionAvailable, string? LatestVersion, string? DownloadUrl)> CheckForUpdatesAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, RepoUrl);
            request.Headers.Add("User-Agent", "MAMEUtility-App");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("tag_name", out var tagElement))
            {
                var latestVersionTag = tagElement.GetString() ?? string.Empty;
                var latestVersionStr = latestVersionTag.TrimStart('v');
                var currentVersionStr = _versionService.RawVersion;

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
        catch (Exception ex)
        {
            // Report the bug as per user requirement
            var bugReportService = ServiceLocator.Instance.Resolve<IBugReportService>();
            await bugReportService.SendExceptionReportAsync(ex);
            return (false, null, null);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
