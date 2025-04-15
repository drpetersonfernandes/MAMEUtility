using MAMEUtility.Services.Interfaces;
using MAMEUtility.ViewModels;

namespace MAMEUtility;

/// <summary>
/// Simple service locator for dependency injection
/// </summary>
public class ServiceLocator
{
    private static ServiceLocator? _instance;
    private readonly Dictionary<Type, object> _services = new();

    /// <summary>
    /// Gets the singleton instance of the ServiceLocator
    /// </summary>
    public static ServiceLocator Instance => _instance ??= new ServiceLocator();

    /// <summary>
    /// Private constructor to enforce singleton pattern
    /// </summary>
    private ServiceLocator()
    {
        // Register services
        RegisterServices();

        // Register view models
        RegisterViewModels();
    }

    /// <summary>
    /// Registers the services
    /// </summary>
    private void RegisterServices()
    {
        // AppConfig
        var appConfig = AppConfig.Instance;

        // BugReportService
        var bugReportService = new Services.BugReportService(
            appConfig.BugReportApiUrl,
            appConfig.BugReportApiKey);
        Register<IBugReportService>(bugReportService);

        // LogService
        var logService = new Services.LogService(bugReportService);
        Register<ILogService>(logService);

        // DialogService
        var dialogService = new Services.DialogService();
        Register<IDialogService>(dialogService);

        // MameProcessingService
        var mameProcessingService = new Services.MameProcessingService(logService);
        Register<IMameProcessingService>(mameProcessingService);
    }

    /// <summary>
    /// Registers the view models
    /// </summary>
    private void RegisterViewModels()
    {
        // MainViewModel
        var mainViewModel = new MainViewModel(
            Resolve<ILogService>(),
            Resolve<IDialogService>(),
            Resolve<IMameProcessingService>());
        Register(mainViewModel);

        // AboutViewModel
        var aboutViewModel = new AboutViewModel(Resolve<ILogService>());
        Register(aboutViewModel);

        // LogViewModel
        var logViewModel = new LogViewModel(Resolve<ILogService>());
        Register(logViewModel);
    }

    /// <summary>
    /// Registers a service
    /// </summary>
    /// <typeparam name="T">Type of the service</typeparam>
    /// <param name="service">Service instance</param>
    public void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    /// <summary>
    /// Resolves a service
    /// </summary>
    /// <typeparam name="T">Type of the service</typeparam>
    /// <returns>Service instance</returns>
    public T Resolve<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }
}