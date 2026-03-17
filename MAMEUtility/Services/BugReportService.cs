using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MAMEUtility.Interfaces;

namespace MAMEUtility.Services;

public class BugReportService : IBugReportService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;
    private readonly IVersionService _versionService;

    public BugReportService(HttpClient httpClient, string apiUrl, string apiKey, IVersionService versionService, string applicationName = "MAME Utility")
    {
        _httpClient = httpClient;
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _versionService = versionService;
        _applicationName = applicationName;
    }

    public async Task SendExceptionReportAsync(Exception exception)
    {
        try
        {
            var message = FormatExceptionMessage(exception);
            await SendReportAsync(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send bug report: {ex.Message}");
            // Silently fail if we can't send the bug report,
            // We don't want errors in the error reporting to cause more issues
        }
    }

    private async Task SendReportAsync(string message)
    {
        try
        {
            var content = new
            {
                message,
                applicationName = _applicationName
            };

            var json = JsonSerializer.Serialize(content);
            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
            {
                Content = stringContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in SendReportAsync: {ex.Message}");
            // Silently fail if sending fails - cannot report bug report failures to avoid recursion
        }
    }

    private string FormatExceptionMessage(Exception exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Exception: {exception.GetType().Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Stack Trace: {exception.StackTrace}");

        // Add version info from centralized service
        sb.AppendLine(CultureInfo.InvariantCulture, $"App Version: {_versionService.ApplicationVersion}");

        // Add OS info
        sb.AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");

        // Add additional information about inner exceptions if present
        var innerException = exception.InnerException;
        if (innerException == null) return sb.ToString();

        sb.AppendLine("\nInner Exception:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Type: {innerException.GetType().Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {innerException.Message}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Stack Trace: {innerException.StackTrace}");

        return sb.ToString();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
