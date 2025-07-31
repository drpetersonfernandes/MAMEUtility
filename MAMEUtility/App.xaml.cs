using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public partial class App : IDisposable
{
    private ILogService? _logService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Initialize service locator
            var serviceLocator = ServiceLocator.Instance;

            // Get the log service
            _logService = serviceLocator.Resolve<ILogService>();

            // Initialize LogWindow
            _logService.ShowLogWindow();
        }
        catch (Exception ex)
        {
            // Fallback logging if LogService initialization fails
            Debug.WriteLine($"CRITICAL ERROR: Failed to initialize LogService: {ex}");
            MessageBox.Show(
                $"A critical error occurred during application startup and the logging service could not be initialized.\n\nDetails: {ex.Message}\n\nApplication will attempt to continue, but errors may not be fully logged.",
                "Critical Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            // _logService remains null, LogAndReportException will handle it.
        }

        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogAndReportException(exception, "AppDomain.CurrentDomain.UnhandledException");
        }
    }

    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogAndReportException(e.Exception, "DispatcherUnhandledException");

        // Mark as handled to prevent application from crashing
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogAndReportException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private void LogAndReportException(Exception exception, string context = "")
    {
        try
        {
            // Always log to debug output as a fallback
            Debug.WriteLine($"Unhandled Exception ({context}): {exception}");

            if (_logService != null)
            {
                _ = _logService.LogExceptionAsync(exception, context);
            }
            else
            {
                // If logService is null, show a basic message to the user for critical errors
                MessageBox.Show(
                    $"An unhandled error occurred: {exception.Message}\n\n" +
                    "The logging service is not available. Please check debug output for more details.",
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        catch (Exception ex)
        {
            // Ignore exceptions in the exception handler, but log to debug as last resort
            Debug.WriteLine($"CRITICAL: Exception occurred in LogAndReportException: {ex}");
            Debug.WriteLine($"Original exception: {exception}");
        }
    }

    public void Dispose()
    {
        // Unregister from event handlers to prevent memory leaks
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        // Suppress finalization
        GC.SuppressFinalize(this);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        // Explicitly call Dispose when the application exits
        // This ensures resources are cleaned up, especially event subscriptions.
        Dispose();
    }
}
