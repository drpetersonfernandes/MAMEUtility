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
    private readonly HttpClient _httpClient = new();

    public ApplicationStatsService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task SendStartStatsAsync()
    {
        try
        {
            var content = new
            {
                applicationId = ApplicationId,
                version = AboutWindow.ApplicationVersion
            };

            var json = JsonSerializer.Serialize(content);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            await _httpClient.PostAsync(ApiUrl, stringContent);
            // We don't strictly need to ensure success, stats collection should be silent
        }
        catch
        {
            // Silently fail if sending fails
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
