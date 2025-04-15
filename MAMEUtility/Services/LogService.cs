using System.Diagnostics;
using System.IO;
using System.Text; // Added for StringBuilder
using System.Globalization; // Added for CultureInfo
using System.Windows;
using System.Windows.Threading;
using MAMEUtility.Services.Interfaces;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MAMEUtility.Services;

public class LogService : ILogService, IDisposable
{
    private readonly IBugReportService _bugReportService;
    private readonly string _logFilePath;
    private LogWindow? _logWindow;
    private readonly Dispatcher _dispatcher;
    private readonly SemaphoreSlim _logFileSemaphore = new(1, 1); // For thread-safe file access

    public event EventHandler<string>? LogMessageAdded;

    public LogService(IBugReportService bugReportService)
    {
        _bugReportService = bugReportService;
        _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MAMEUtilityLog.txt");

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
            Debug.WriteLine($"Error initializing log directory/file: {ex.Message}");
        }
    }

    public void ShowLogWindow()
    {
        // Ensure this runs on the UI thread
        if (_dispatcher.CheckAccess()) return;

        _dispatcher.Invoke(ShowLogWindow);
    }

    public void Log(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var formattedMessage = FormatLogMessage(message, LogLevel.Info);
            AppendToLogWindow(formattedMessage);
            _ = LogToFileAsync(formattedMessage);

            _dispatcher.BeginInvoke(() => LogMessageAdded?.Invoke(this, message));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during Log operation: {ex.Message}");
            _ = LogToFileAsync($"[LogService Internal Error] Failed during Log: {ex.Message}{Environment.NewLine}");
        }
    }

    public void LogError(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var formattedMessage = FormatLogMessage(message, LogLevel.Error);
            AppendToLogWindow(formattedMessage);
            _ = LogToFileAsync(formattedMessage);

            _dispatcher.BeginInvoke(() =>
            {
                AskUserToOpenLogFile();
                LogMessageAdded?.Invoke(this, message);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during LogError operation: {ex.Message}");
            _ = LogToFileAsync($"[LogService Internal Error] Failed during LogError: {ex.Message}{Environment.NewLine}");
        }
    }

    public void LogWarning(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var formattedMessage = FormatLogMessage(message, LogLevel.Warning);
            AppendToLogWindow(formattedMessage);
            _ = LogToFileAsync(formattedMessage);

            _dispatcher.BeginInvoke(() =>
            {
                AskUserToOpenLogFile(MessageBoxImage.Warning);
                LogMessageAdded?.Invoke(this, message);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during LogWarning operation: {ex.Message}");
            _ = LogToFileAsync($"[LogService Internal Error] Failed during LogWarning: {ex.Message}{Environment.NewLine}");
        }
    }

    public async Task LogExceptionAsync(Exception exception, string additionalInfo = "")
    {
        try
        {
            if (exception == null)
            {
                LogWarning("LogExceptionAsync called with null exception.");
                return;
            }

            var errorMessage = FormatExceptionMessage(exception, additionalInfo);
            var userMessage = $"Error: {exception.GetType().Name} - {exception.Message}";

            AppendToLogWindow(userMessage);
            await LogToFileAsync(errorMessage);
            await _bugReportService.SendExceptionReportAsync(exception);

            AskUserToOpenLogFile();
            LogMessageAdded?.Invoke(this, userMessage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during LogExceptionAsync: {ex.Message}");
            try
            {
                var fallbackMsg = $"[LogService Internal Error] Error in LogExceptionAsync: {ex.Message}{Environment.NewLine}" +
                                  $"Original exception: {exception?.GetType().Name} - {exception?.Message}{Environment.NewLine}";
                await LogToFileAsync(fallbackMsg);
            }
            catch
            {
                Debug.WriteLine("Critical failure in logging.");
                if (exception != null) Debug.WriteLine($"Original exception: {exception.Message}");
            }
        }
    }

    private void AppendToLogWindow(string formattedMessage)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => AppendToLogWindowInternal(formattedMessage));

            return;
        }

        AppendToLogWindowInternal(formattedMessage);
    }

    private void AppendToLogWindowInternal(string formattedMessage)
    {
        try
        {
            if (_logWindow is { IsLoaded: true })
            {
                _logWindow.AppendLog(formattedMessage.TrimEnd(Environment.NewLine.ToCharArray()));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error appending to log window: {ex.Message}");
        }
    }

    private async Task LogToFileAsync(string formattedMessage)
    {
        await _logFileSemaphore.WaitAsync();
        try
        {
            await using var writer = new StreamWriter(_logFilePath, true, Encoding.UTF8);
            await writer.WriteAsync(formattedMessage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing to log file '{_logFilePath}': {ex.Message}");
        }
        finally
        {
            _logFileSemaphore.Release();
        }
    }

    private static string FormatLogMessage(string message, LogLevel level)
    {
        return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-7}] {message}{Environment.NewLine}";
    }

    private static string FormatExceptionMessage(Exception exception, string additionalInfo = "")
    {
        var sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        sb.Append(" [Exception] ");
        if (!string.IsNullOrWhiteSpace(additionalInfo))
        {
            sb.Append(CultureInfo.InvariantCulture, $"Context: {additionalInfo} | ");
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.GetType().FullName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
        sb.AppendLine("--- Stack Trace ---");
        sb.AppendLine(exception.StackTrace ?? "No stack trace available.");
        sb.AppendLine("--- End Stack Trace ---");

        var inner = exception.InnerException;
        var innerLevel = 1;
        while (inner != null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"\n--- Inner Exception Level {innerLevel} ---");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Type: {inner.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {inner.Message}");
            sb.AppendLine("--- Inner Stack Trace ---");
            sb.AppendLine(inner.StackTrace ?? "No inner stack trace available.");
            sb.AppendLine("--- End Inner Stack Trace ---");
            inner = inner.InnerException;
            innerLevel++;
        }

        sb.AppendLine("--- End Exception Details ---");
        sb.AppendLine(); // Add a blank line for separation in the log file

        return sb.ToString();
    }

    private void AskUserToOpenLogFile(MessageBoxImage messageBoxImage = MessageBoxImage.Error)
    {
        if (!_dispatcher.CheckAccess())
        {
            Debug.WriteLine("Warning: AskUserToOpenLogFile called from non-UI thread directly.");
            _dispatcher.BeginInvoke(() => AskUserToOpenLogFileInternal(messageBoxImage));

            return;
        }

        AskUserToOpenLogFileInternal(messageBoxImage);
    }

    private void AskUserToOpenLogFileInternal(MessageBoxImage messageBoxImage)
    {
        try
        {
            var title = messageBoxImage == MessageBoxImage.Warning ? "Warning Occurred" : "Error Occurred";
            var message = $"A {(messageBoxImage == MessageBoxImage.Warning ? "warning" : "critical error")} has occurred and has been logged.\n\nWould you like to open the log file?\n({_logFilePath})";

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, messageBoxImage);

            if (result != MessageBoxResult.Yes) return;

            if (!File.Exists(_logFilePath))
            {
                MessageBox.Show($"Log file not found at:\n{_logFilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var psi = new ProcessStartInfo(_logFilePath)
            {
                UseShellExecute = true // Necessary to open with default app
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing 'Open Log File' dialog or opening file: {ex.Message}");
            try
            {
                MessageBox.Show($"Could not open log file: {ex.Message}", "Error Opening Log", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = LogToFileAsync($"[LogService Internal Error] Failed to open log file via dialog: {ex.Message}{Environment.NewLine}");
            }
            catch
            {
                /* Silently fail */
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logFileSemaphore?.Dispose();
        }

        _logWindow = null;
        _disposed = true;
    }
}