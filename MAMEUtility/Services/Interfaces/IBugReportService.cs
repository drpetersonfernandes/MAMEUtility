namespace MAMEUtility.Services.Interfaces;

/// <inheritdoc />
/// <summary>
/// Service for sending bug reports
/// </summary>
public interface IBugReportService : IDisposable
{
    /// <summary>
    /// Sends an exception report to the bug reporting service
    /// </summary>
    /// <param name="exception">Exception to report</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task SendExceptionReportAsync(Exception exception);
}