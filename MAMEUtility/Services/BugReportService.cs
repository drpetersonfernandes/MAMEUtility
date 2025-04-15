using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility.Services;

/// <inheritdoc />
/// <summary>
/// Implementation of the IBugReportService interface
/// </summary>
public class BugReportService : IBugReportService
{
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiUrl">API URL for bug reporting</param>
    /// <param name="apiKey">API key for bug reporting</param>
    /// <param name="applicationName">Application name</param>
    public BugReportService(string apiUrl, string apiKey, string applicationName = "MAME Utility")
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
    }

    /// <inheritdoc />
    /// <summary>
    /// Sends an exception report to the bug reporting service
    /// </summary>
    /// <param name="exception">Exception to report</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task SendExceptionReportAsync(Exception exception)
    {
        try
        {
            var message = FormatExceptionMessage(exception);
            await SendReportAsync(message);
        }
        catch
        {
            // Silently fail if we can't send the bug report,
            // We don't want errors in the error reporting to cause more issues
        }
    }

    /// <summary>
    /// Sends a report to the bug reporting service
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <returns>Task representing the asynchronous operation</returns>
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
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);

            var response = await _httpClient.PostAsync(_apiUrl, stringContent);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Silently fail if sending fails
        }
    }

    /// <summary>
    /// Formats an exception message for bug reporting
    /// </summary>
    /// <param name="exception">Exception to format</param>
    /// <returns>Formatted message</returns>
    private static string FormatExceptionMessage(Exception exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Exception: {exception.GetType().Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Stack Trace: {exception.StackTrace}");

        // Add version info
        sb.AppendLine(CultureInfo.InvariantCulture, $"App Version: {AboutWindow.ApplicationVersion}");

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

    /// <inheritdoc />
    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        // Dispose of the HttpClient to release resources
        _httpClient?.Dispose();

        // Suppress finalization
        GC.SuppressFinalize(this);
    }
}