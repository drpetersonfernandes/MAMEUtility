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

        // Create BugReportService with default config first (needed for AppConfig error reporting)
        var tempBugReportService = new BugReportService(
            SharedHttpClient,
            "https://www.purelogiccode.com/bugreport/api/send-bug-report",
            "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e",
            versionService);

        // Subscribe to AppConfig load errors BEFORE accessing AppConfig.Instance
        // This ensures errors during config loading are captured
        AppConfig.ConfigLoadError += (sender, ex) =>
        {
            // Fire and forget is acceptable here as this is during startup
            _ = tempBugReportService.SendExceptionReportAsync(ex);
        };

        // AppConfig - LoadConfig() runs here via Lazy initialization
        var appConfig = AppConfig.Instance;

        // BugReportService - now with actual config values
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