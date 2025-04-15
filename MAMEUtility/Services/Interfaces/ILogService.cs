namespace MAMEUtility.Services.Interfaces;

/// <summary>
/// Service for logging messages and errors
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Logs a regular message
    /// </summary>
    /// <param name="message">Message to log</param>
    void Log(string message);

    /// <summary>
    /// Logs an error message
    /// </summary>
    /// <param name="message">Error message to log</param>
    void LogError(string message);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="message">Warning message to log</param>
    void LogWarning(string message);

    /// <summary>
    /// Logs an exception
    /// </summary>
    /// <param name="exception">Exception to log</param>
    /// <param name="additionalInfo">Additional contextual information</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task LogExceptionAsync(Exception exception, string additionalInfo = "");

    /// <summary>
    /// Shows the log window
    /// </summary>
    void ShowLogWindow();

    /// <summary>
    /// Event that is raised when a new log message is added
    /// </summary>
    event EventHandler<string> LogMessageAdded;
}