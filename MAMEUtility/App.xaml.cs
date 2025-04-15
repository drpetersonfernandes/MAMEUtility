using System.Windows;
using System.Windows.Threading;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

/// <inheritdoc cref="System.Windows.Application" />
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : IDisposable
{
    private ILogService? _logService;

    /// <inheritdoc />
    /// <summary>
    /// Called when the application starts up
    /// </summary>
    /// <param name="e">Startup event arguments</param>
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

    /// <summary>
    /// Handles unhandled exceptions in the AppDomain
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogAndReportException(exception);
        }
    }

    /// <summary>
    /// Handles unhandled exceptions in the Dispatcher
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogAndReportException(e.Exception);

        // Mark as handled to prevent application from crashing
        e.Handled = true;
    }

    /// <summary>
    /// Handles unobserved task exceptions
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogAndReportException(e.Exception);
        e.SetObserved();
    }

    /// <summary>
    /// Logs and reports an exception
    /// </summary>
    /// <param name="exception">Exception to log and report</param>
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

    /// <inheritdoc />
    /// <summary>
    /// Disposes resources
    /// </summary>
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