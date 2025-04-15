using System.Windows;
using System.Windows.Threading;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

public partial class App : IDisposable
{
    private ILogService? _logService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize service locator
        var serviceLocator = ServiceLocator.Instance;

        // Get the log service
        _logService = serviceLocator.Resolve<ILogService>();

        // Initialize LogWindow
        _logService.ShowLogWindow();

        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogAndReportException(exception);
        }
    }

    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogAndReportException(e.Exception);

        // Mark as handled to prevent application from crashing
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogAndReportException(e.Exception);
        e.SetObserved();
    }

    private void LogAndReportException(Exception exception)
    {
        try
        {
            if (_logService != null)
            {
                _ = _logService.LogExceptionAsync(exception);
            }
        }
        catch
        {
            // Ignore exceptions in the exception handler
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
}