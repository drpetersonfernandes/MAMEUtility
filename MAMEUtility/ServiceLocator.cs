using System.Net.Http;
using MAMEUtility.Interfaces;
using MAMEUtility.Services;

namespace MAMEUtility;

public class ServiceLocator
{
    private static readonly Lazy<ServiceLocator> Instance2 = new(static () => new ServiceLocator());
    private readonly Dictionary<Type, object> _services = new();
    private static readonly HttpClient SharedHttpClient = new();

    public static ServiceLocator Instance => Instance2.Value;

    private ServiceLocator()
    {
        // Register services
        RegisterServices();
    }

    private void RegisterServices()
    {
        // VersionService - register first as it's needed by other services
        var versionService = new VersionService();
        Register<IVersionService>(versionService);

        // AppConfig
        var appConfig = AppConfig.Instance;

        // BugReportService
        var bugReportService = new BugReportService(
            SharedHttpClient,
            appConfig.BugReportApiUrl,
            appConfig.BugReportApiKey,
            versionService);
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

        // ApplicationStatsService
        var appStatsService = new ApplicationStatsService(SharedHttpClient, appConfig.BugReportApiKey, versionService);
        Register<IApplicationStatsService>(appStatsService);

        // VersionCheckService
        var versionCheckService = new GitHubVersionService(SharedHttpClient, versionService);
        Register<IVersionCheckService>(versionCheckService);

        // Subscribe to AppConfig load errors
        AppConfig.ConfigLoadError += (sender, ex) =>
        {
            // Fire and forget is acceptable here as this is during startup
            _ = bugReportService.SendExceptionReportAsync(ex);
        };
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