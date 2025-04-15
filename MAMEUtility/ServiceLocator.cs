using MAMEUtility.Services.Interfaces;
using MAMEUtility.ViewModels;

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

        // Register view models
        RegisterViewModels();
    }

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