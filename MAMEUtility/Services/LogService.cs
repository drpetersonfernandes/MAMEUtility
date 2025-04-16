using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;
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

        // Get the UI thread dispatcher - Ensure Application.Current is not null
        if (Application.Current == null)
        {
            // This might happen in rare scenarios like unit testing or if called too early
            throw new InvalidOperationException("Application.Current is null. LogService cannot be initialized without a running WPF application.");
        }

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
        // Ensure the operation runs on the UI thread
        if (!_dispatcher.CheckAccess())
        {
            // If not on the UI thread, invoke the internal method
            _dispatcher.Invoke(ShowLogWindowInternal);
        }
        else
        {
            // Already on the UI thread, call the internal method directly
            ShowLogWindowInternal();
        }
    }

    private void ShowLogWindowInternal()
    {
        try
        {
            // Check if the window already exists and is loaded/visible
            if (_logWindow is { IsLoaded: true, IsVisible: true })
            {
                _logWindow.Activate(); // Bring the existing window to the front
                return;
            }

            // If the window exists but was closed (IsLoaded becomes false), clear the reference
            if (_logWindow is { IsLoaded: false })
            {
                _logWindow = null;
            }

            // If the window doesn't exist or was closed, create a new instance
            if (_logWindow == null)
            {
                _logWindow = new LogWindow();
                // Handle the Closed event to nullify the reference
                // This prevents issues if the user closes the window and tries to reopen it
                _logWindow.Closed += (sender, args) => { _logWindow = null; };
            }

            _logWindow.Show();
            _logWindow.Activate(); // Ensure it gets focus
        }
        catch (Exception ex)
        {
            LogErrorInternal($"Failed to show Log Window: {ex.Message}");
            _ = LogExceptionAsync(ex, "Error in ShowLogWindowInternal");
            MessageBox.Show($"Could not display the log window. Please check the log file for details.\nError: {ex.Message}",
                "Log Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Log(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var formattedMessage = FormatLogMessage(message, LogLevel.Info);
            _ = LogToFileAsync(formattedMessage);

            // Raise event on UI thread - ViewModel will handle updating the window
            _dispatcher.BeginInvoke(() => LogMessageAdded?.Invoke(this, formattedMessage.TrimEnd(Environment.NewLine.ToCharArray())));
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
            var userMessage = $"Error: {message}"; // Message for UI/event

            _ = LogToFileAsync(formattedMessage);

            _dispatcher.BeginInvoke(() =>
            {
                LogMessageAdded?.Invoke(this, userMessage); // Use userMessage for the event
                AskUserToOpenLogFile(); // Ask on UI thread
            });
        }
        catch (Exception ex)
        {
            LogErrorInternal($"Error during LogError operation: {ex.Message}");
        }
    }

    // Internal version to avoid recursion if LogError fails
    private void LogErrorInternal(string message)
    {
        Debug.WriteLine(message);
        try
        {
            var formattedMessage = FormatLogMessage(message, LogLevel.Error);
            _ = LogToFileAsync($"[LogService Internal Error] {formattedMessage}");
        }
        catch
        {
            /* Last resort */
        }
    }


    public void LogWarning(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var formattedMessage = FormatLogMessage(message, LogLevel.Warning);
            var userMessage = $"Warning: {message}"; // Message for UI/event

            // REMOVED: AppendToLogWindow(formattedMessage); // This was causing the duplicate
            _ = LogToFileAsync(formattedMessage);

            _dispatcher.BeginInvoke(() =>
            {
                LogMessageAdded?.Invoke(this, userMessage); // Use userMessage for the event
                AskUserToOpenLogFile(MessageBoxImage.Warning); // Ask on UI thread
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

            // REMOVED: AppendToLogWindow(errorMessage); // This was causing the duplicate
            await LogToFileAsync(errorMessage); // Log full details to file
            await _bugReportService.SendExceptionReportAsync(exception);

            // Raise event and ask user on UI thread
            _ = _dispatcher.BeginInvoke(() =>
            {
                LogMessageAdded?.Invoke(this, userMessage); // Use userMessage for the event
                AskUserToOpenLogFile();
            });
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
        // Add newline consistently for file logging
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
        // This method already correctly uses BeginInvoke if needed.
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
            // Ensure the log window is closed if the service is disposed
            _dispatcher?.Invoke(() => _logWindow?.Close());
        }

        _logWindow = null;
        _disposed = true;
    }
}