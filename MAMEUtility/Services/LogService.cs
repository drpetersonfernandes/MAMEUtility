using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using MAMEUtility.Services.Interfaces;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MAMEUtility.Services;

/// <inheritdoc cref="System.IDisposable" />
/// <summary>
/// Implementation of the ILogService interface
/// </summary>
public class LogService : ILogService, IDisposable
{
    private readonly IBugReportService _bugReportService;
    private readonly string _logFilePath;
    private LogWindow? _logWindow;
    private readonly Dispatcher _dispatcher;
    private readonly SemaphoreSlim _logFileSemaphore = new(1, 1); // For thread-safe file access

    /// <inheritdoc />
    /// <summary>
    /// Event raised when a log message is added
    /// </summary>
    public event EventHandler<string>? LogMessageAdded;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bugReportService">Bug report service</param>
    public LogService(IBugReportService bugReportService)
    {
        _bugReportService = bugReportService;
        _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorLog.txt");

        // Get the UI thread dispatcher
        _dispatcher = Application.Current.Dispatcher;

        // Create log directory if it doesn't exist
        try
        {
            var logDirectory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }
        catch (Exception ex)
        {
            // Just write to debug output since we can't log yet
            Debug.WriteLine($"Error creating log directory: {ex.Message}");
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows the log window
    /// </summary>
    public void ShowLogWindow()
    {
        // Ensure this runs on the UI thread
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.Invoke(ShowLogWindow);
            return;
        }

        if (_logWindow == null)
        {
            _logWindow = new LogWindow();
            _logWindow.Closed += (_, _) => { _logWindow = null; };
            _logWindow.Show();
        }
        else
        {
            _logWindow.Activate();
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Logs a regular message
    /// </summary>
    /// <param name="message">Message to log</param>
    public async void Log(string message)
    {
        try
        {
            // Don't log empty messages
            if (string.IsNullOrWhiteSpace(message))
                return;

            AppendToLogWindow(message);

            // Using await instead of fire-and-forget to prevent warning
            await LogToFileAsync(FormatLogMessage(message, LogLevel.Info));

            // Ensure event is raised on the UI thread
            _dispatcher.BeginInvoke(() => LogMessageAdded?.Invoke(this, message));
        }
        catch (Exception)
        {
            // TODO handle exception
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Logs an error message
    /// </summary>
    /// <param name="message">Error message to log</param>
    public async void LogError(string message)
    {
        try
        {
            // Don't log empty messages
            if (string.IsNullOrWhiteSpace(message))
                return;

            AppendToLogWindow(message);

            // Using await instead of fire-and-forget to prevent warning
            await LogToFileAsync(FormatLogMessage(message, LogLevel.Error));

            // Ensure UI interaction happens on the UI thread
            _dispatcher.BeginInvoke(() =>
            {
                AskUserToOpenLogFile();
                LogMessageAdded?.Invoke(this, message);
            });
        }
        catch (Exception)
        {
            // TODO handle exception
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="message">Warning message to log</param>
    public async void LogWarning(string message)
    {
        try
        {
            // Don't log empty messages
            if (string.IsNullOrWhiteSpace(message))
                return;

            AppendToLogWindow(message);

            // Using await instead of fire-and-forget to prevent warning
            await LogToFileAsync(FormatLogMessage(message, LogLevel.Warning));

            // Ensure UI interaction happens on the UI thread
            _dispatcher.BeginInvoke(() =>
            {
                AskUserToOpenLogFile(MessageBoxImage.Warning);
                LogMessageAdded?.Invoke(this, message);
            });
        }
        catch (Exception)
        {
            // TODO handle exception
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Logs an exception
    /// </summary>
    /// <param name="exception">Exception to log</param>
    /// <param name="additionalInfo">Additional contextual information</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task LogExceptionAsync(Exception exception, string additionalInfo = "")
    {
        try
        {
            // Format the error message
            var errorMessage = FormatExceptionMessage(exception, additionalInfo);

            // Append to log window
            AppendToLogWindow($"Error: {exception.Message}");

            // Log to file
            await LogToFileAsync(errorMessage);

            // Send to API
            await _bugReportService.SendExceptionReportAsync(exception);

            // Ask user if they want to open the log file - must be on UI thread
            _dispatcher.BeginInvoke(() =>
            {
                AskUserToOpenLogFile();
                LogMessageAdded?.Invoke(this, $"Error: {exception.Message}");
            });
        }
        catch (Exception ex)
        {
            // If an error occurs during logging, at least try to write it to the file
            try
            {
                await LogToFileAsync($"Error in logging: {ex.Message}\nOriginal exception: {exception.Message}\n");
            }
            catch
            {
                // Last resort - write to debug output
                Debug.WriteLine($"Critical failure in logging: {ex.Message}");
                Debug.WriteLine($"Original exception: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Appends a message to the log window
    /// </summary>
    /// <param name="message">Message to append</param>
    private void AppendToLogWindow(string message)
    {
        if (_logWindow == null) return;

        // Use Dispatcher to ensure UI updates happen on the UI thread
        _dispatcher.BeginInvoke(() => _logWindow.AppendLog(message));
    }

    /// <summary>
    /// Logs a message to the error log file
    /// </summary>
    /// <param name="message">Message to log</param>
    /// <returns>Task representing the asynchronous operation</returns>
    private async Task LogToFileAsync(string message)
    {
        // Use semaphore for thread-safe file access
        await _logFileSemaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logFilePath, message);
        }
        catch (Exception ex)
        {
            // Write to debug output since we can't log the logging error
            Debug.WriteLine($"Error writing to log file: {ex.Message}");
        }
        finally
        {
            _logFileSemaphore.Release();
        }
    }

    /// <summary>
    /// Formats a log message
    /// </summary>
    /// <param name="message">Message to format</param>
    /// <param name="level">Log level</param>
    /// <returns>Formatted message</returns>
    private static string FormatLogMessage(string message, LogLevel level)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Level: {level}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {message}");

        // Add version info
        sb.AppendLine(CultureInfo.InvariantCulture, $"App Version: {AboutWindow.ApplicationVersion}");

        sb.AppendLine(new string('-', 80)); // Separator line
        return sb.ToString();
    }

    /// <summary>
    /// Formats an exception message for logging
    /// </summary>
    /// <param name="exception">Exception to format</param>
    /// <param name="additionalInfo">Additional contextual information</param>
    /// <returns>Formatted message</returns>
    private static string FormatExceptionMessage(Exception exception, string additionalInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

        if (!string.IsNullOrEmpty(additionalInfo))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Additional Info: {additionalInfo}");
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"Exception: {exception.GetType().Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Stack Trace: {exception.StackTrace}");

        // Add version info
        sb.AppendLine(CultureInfo.InvariantCulture, $"App Version: {AboutWindow.ApplicationVersion}");

        // Add OS info
        sb.AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");

        // Add inner exception info if present
        if (exception.InnerException != null)
        {
            sb.AppendLine("\nInner Exception:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.InnerException.GetType().Name}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.InnerException.Message}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Stack Trace: {exception.InnerException.StackTrace}");
        }

        sb.AppendLine(new string('-', 80)); // Separator line
        return sb.ToString();
    }

    /// <summary>
    /// Asks the user if they want to open the error log file
    /// </summary>
    /// <param name="messageBoxImage">Type of message box to show</param>
    private void AskUserToOpenLogFile(MessageBoxImage messageBoxImage = MessageBoxImage.Error)
    {
        // This should only be called from the UI thread
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.Invoke(() => AskUserToOpenLogFile(messageBoxImage));
            return;
        }

        var title = messageBoxImage == MessageBoxImage.Warning ? "Warning Occurred" : "Error Occurred";
        var message = $"A {(messageBoxImage == MessageBoxImage.Warning ? "warning" : "error")} has occurred and has been logged. Would you like to open the log file?";

        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            messageBoxImage);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            // Open the log file with the default application
            Process.Start(new ProcessStartInfo
            {
                FileName = _logFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open log file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Disposes managed resources
    /// </summary>
    public void Dispose()
    {
        _logFileSemaphore.Dispose();

        GC.SuppressFinalize(this);
    }
}