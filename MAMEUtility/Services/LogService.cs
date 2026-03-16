using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using MAMEUtility.Interfaces;
using MAMEUtility.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MAMEUtility.Services;

public class LogService : ILogService, IDisposable
{
    private readonly IBugReportService _bugReportService;
    private readonly string _logFilePath;
    private LogWindow? _logWindow;
    private readonly Dispatcher? _dispatcher;
    private readonly SemaphoreSlim _logFileSemaphore = new(1, 1);
    private bool _isBatchOperation;
    private bool _errorsOccurredInBatch;
    private bool _warningsOccurredInBatch;

    public event EventHandler<string>? LogMessageAdded;

    public LogService(IBugReportService bugReportService)
    {
        _bugReportService = bugReportService;
        _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MAMEUtilityLog.txt");
        _dispatcher = Application.Current?.Dispatcher;

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
            Debug.WriteLine($"Error initializing log directory: {ex.Message}");
        }
    }

    public void ShowLogWindow()
    {
        if (_dispatcher?.CheckAccess() == false)
        {
            _dispatcher.Invoke(ShowLogWindowInternal);
        }
        else
        {
            ShowLogWindowInternal();
        }
    }

    private void ShowLogWindowInternal()
    {
        try
        {
            switch (_logWindow)
            {
                case { IsLoaded: true, IsVisible: true }:
                    _logWindow.Activate();
                    return;
                case { IsLoaded: false }:
                    _logWindow = null;
                    break;
            }

            if (_logWindow == null)
            {
                _logWindow = new LogWindow();
                _logWindow.Closed += (_, _) => { _logWindow = null; };
            }

            _logWindow.Show();
            _logWindow.Activate();
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
        if (string.IsNullOrWhiteSpace(message)) return;

        var formattedMessage = FormatLogMessage(message, LogLevel.Info);
        _ = LogToFileAsync(formattedMessage);

        _dispatcher?.BeginInvoke(() =>
        {
            LogMessageAdded?.Invoke(this, formattedMessage);
        });
    }

    public void LogError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var formattedMessage = FormatLogMessage(message, LogLevel.Error);
        _ = LogToFileAsync(formattedMessage);

        _dispatcher?.BeginInvoke(() =>
        {
            LogMessageAdded?.Invoke(this, formattedMessage);

            if (_isBatchOperation)
            {
                _errorsOccurredInBatch = true;
            }
            else
            {
                NotifyUser("Error Occurred", message, MessageBoxImage.Error);
            }
        });
    }

    public void LogWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var formattedMessage = FormatLogMessage(message, LogLevel.Warning);
        _ = LogToFileAsync(formattedMessage);

        _dispatcher?.BeginInvoke(() =>
        {
            LogMessageAdded?.Invoke(this, formattedMessage);

            if (_isBatchOperation)
            {
                _warningsOccurredInBatch = true;
            }
            else
            {
                NotifyUser("Warning Occurred", message, MessageBoxImage.Warning);
            }
        });
    }

    public async Task LogExceptionAsync(Exception exception, string additionalInfo = "")
    {
        var errorMessage = FormatExceptionMessage(exception, additionalInfo);
        await LogToFileAsync(errorMessage);
        await _bugReportService.SendExceptionReportAsync(exception);

        _dispatcher?.BeginInvoke(() =>
        {
            LogMessageAdded?.Invoke(this, errorMessage);

            if (_isBatchOperation)
            {
                _errorsOccurredInBatch = true;
            }
            else
            {
                NotifyUser("Critical Error Occurred", exception.Message, MessageBoxImage.Error);
            }
        });
    }

    public void BeginBatchOperation()
    {
        _isBatchOperation = true;
        _errorsOccurredInBatch = false;
        _warningsOccurredInBatch = false;
    }

    public void EndBatchOperation(string summaryTitle = "Batch Operation Completed")
    {
        _isBatchOperation = false;

        if (_errorsOccurredInBatch || _warningsOccurredInBatch)
        {
            var image = _errorsOccurredInBatch ? MessageBoxImage.Error : MessageBoxImage.Warning;
            var type = _errorsOccurredInBatch ? "errors" : "warnings";
            var message = $"The batch operation completed, but some {type} occurred. Would you like to open the log file to review them?";

            _dispatcher?.Invoke(() =>
            {
                var result = MessageBox.Show(message, summaryTitle, MessageBoxButton.YesNo, image);
                if (result == MessageBoxResult.Yes)
                {
                    OpenLogFile();
                }
            });
        }

        _errorsOccurredInBatch = false;
        _warningsOccurredInBatch = false;
    }

    private void NotifyUser(string title, string message, MessageBoxImage image)
    {
        var fullMessage = $"{message}\n\nWould you like to open the log file?\n({_logFilePath})";
        var result = MessageBox.Show(fullMessage, title, MessageBoxButton.YesNo, image);

        if (result == MessageBoxResult.Yes)
        {
            OpenLogFile();
        }
    }

    private void OpenLogFile()
    {
        try
        {
            if (!File.Exists(_logFilePath))
            {
                MessageBox.Show($"Log file not found at:\n{_logFilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo(_logFilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening log file: {ex.Message}");
            MessageBox.Show($"Could not open log file: {ex.Message}", "Error Opening Log", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogErrorInternal(string message)
    {
        Debug.WriteLine(message);
        var formattedMessage = FormatLogMessage(message, LogLevel.Error);
        _ = LogToFileAsync($"[LogService Internal Error] {formattedMessage}");
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
            Debug.WriteLine($"Error writing to log file: {ex.Message}");
        }
        finally
        {
            _logFileSemaphore.Release();
        }
    }

    private static string FormatLogMessage(string message, LogLevel level)
    {
        return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
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
        sb.AppendLine();
        return sb.ToString();
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
            _logFileSemaphore.Dispose();

            if (_dispatcher != null)
            {
                if (_dispatcher.CheckAccess())
                    _logWindow?.Close();
                else
                    _dispatcher.BeginInvoke(() => _logWindow?.Close());
            }
        }

        _logWindow = null;
        _disposed = true;
    }
}