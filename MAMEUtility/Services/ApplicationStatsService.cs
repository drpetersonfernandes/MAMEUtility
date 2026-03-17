using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MAMEUtility.Interfaces;

namespace MAMEUtility.Services;

public class ApplicationStatsService : IApplicationStatsService, IDisposable
{
    private const string ApiUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private readonly string _apiKey;
    private const string ApplicationId = "mame-utility";
    private readonly HttpClient _httpClient;
    private readonly IVersionService _versionService;

    public ApplicationStatsService(HttpClient httpClient, string apiKey, IVersionService versionService)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _versionService = versionService;
    }

    public async Task SendStartStatsAsync()
    {
        try
        {
            var content = new
            {
                applicationId = ApplicationId,
                version = _versionService.ApplicationVersion
            };

            var json = JsonSerializer.Serialize(content);
            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = stringContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            await _httpClient.SendAsync(request);
            // We don't strictly need to ensure success, stats collection should be silent
        }
        catch (Exception ex)
        {
            // Report bugs as requested
            var bugReportService = ServiceLocator.Instance.Resolve<IBugReportService>();
            await bugReportService.SendExceptionReportAsync(ex);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
