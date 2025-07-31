using MAMEUtility.Interfaces;
using MAMEUtility.Services;

namespace MAMEUtility;

public class ServiceLocator
{
    private static ServiceLocator? _instance;
    private readonly Dictionary<Type, object> _services = new();

    public static ServiceLocator Instance => _instance ??= new ServiceLocator();

    private ServiceLocator()
    {
        // Register services
        RegisterServices();
    }

    private void RegisterServices()
    {
        // AppConfig
        var appConfig = AppConfig.Instance;

        // BugReportService
        var bugReportService = new BugReportService(
            appConfig.BugReportApiUrl,
            appConfig.BugReportApiKey);
        Register<IBugReportService>(bugReportService);

        // LogService
        var logService = new LogService(bugReportService);
        Register<ILogService>(logService);

        // DialogService
        var dialogService = new DialogService();
        Register<IDialogService>(dialogService);

        // MameProcessingService
        var mameProcessingService = new MameProcessingService(logService);
        Register<IMameProcessingService>(mameProcessingService);
    }

    private void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public T Resolve<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }
}