using System.Text;
using System.Globalization;
using System.Windows.Threading;
using MAMEUtility.Interfaces;
using MAMEUtility.Models;
using Application = System.Windows.Application;

namespace MAMEUtility.Services;

public class LogService : ILogService
{
    private readonly IBugReportService _bugReportService;
    private readonly Dispatcher? _dispatcher;
    private readonly object _lock = new();
    private bool _isBatchOperation;
    private bool _errorsOccurredInBatch;
    private bool _warningsOccurredInBatch;

    public event EventHandler<string>? LogMessageAdded;
    public event EventHandler<(string Title, string Message, bool HasErrors)>? BatchOperationCompleted;

    public LogService(IBugReportService bugReportService)
    {
        _bugReportService = bugReportService;
        _dispatcher = Application.Current?.Dispatcher;
    }

    public void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var formattedMessage = FormatLogMessage(message, LogLevel.Info);
        DispatchLog(formattedMessage);
    }

    public void LogError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var formattedMessage = FormatLogMessage(message, LogLevel.Error);

        lock (_lock)
        {
            if (_isBatchOperation)
            {
                _errorsOccurredInBatch = true;
            }
        }

        DispatchLog(formattedMessage);
    }

    public void LogWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var formattedMessage = FormatLogMessage(message, LogLevel.Warning);

        lock (_lock)
        {
            if (_isBatchOperation)
            {
                _warningsOccurredInBatch = true;
            }
        }

        DispatchLog(formattedMessage);
    }

    public async Task LogExceptionAsync(Exception exception, string additionalInfo = "")
    {
        var errorMessage = FormatExceptionMessage(exception, additionalInfo);

        lock (_lock)
        {
            if (_isBatchOperation)
            {
                _errorsOccurredInBatch = true;
            }
        }

        // Report to bug API
        await _bugReportService.SendExceptionReportAsync(exception).ConfigureAwait(false);

        DispatchLog(errorMessage);
    }

    private void DispatchLog(string formattedMessage)
    {
        if (_dispatcher != null)
        {
            _dispatcher.BeginInvoke(() =>
            {
                LogMessageAdded?.Invoke(this, formattedMessage);
            });
        }
        else
        {
            // Fallback for non-UI contexts (unit tests, early startup)
            LogMessageAdded?.Invoke(this, formattedMessage);
            System.Diagnostics.Debug.WriteLine(formattedMessage);
        }
    }

    public void BeginBatchOperation()
    {
        lock (_lock)
        {
            _isBatchOperation = true;
            _errorsOccurredInBatch = false;
            _warningsOccurredInBatch = false;
        }
    }

    public void EndBatchOperation(string summaryTitle = "Batch Operation Completed", bool wasCancelled = false)
    {
        bool errors;
        bool warnings;

        lock (_lock)
        {
            _isBatchOperation = false;
            errors = _errorsOccurredInBatch;
            warnings = _warningsOccurredInBatch;
            _errorsOccurredInBatch = false;
            _warningsOccurredInBatch = false;
        }

        if (!wasCancelled && (errors || warnings))
        {
            var type = errors ? "errors" : "warnings";
            var message = $"The batch operation completed, but some {type} occurred. Please review the log viewer for details.";

            if (_dispatcher != null)
            {
                _dispatcher.BeginInvoke(() =>
                {
                    BatchOperationCompleted?.Invoke(this, (summaryTitle, message, errors));
                });
            }
            else
            {
                BatchOperationCompleted?.Invoke(this, (summaryTitle, message, errors));
                System.Diagnostics.Debug.WriteLine($"[Batch Completed] {summaryTitle}: {message}");
            }
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
        GC.SuppressFinalize(this);
    }
}