using System.IO;
using System.Text.Json;

namespace MAMEUtility;

public class AppConfig
{
    public string BugReportApiUrl { get; set; } = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    public string BugReportApiKey { get; set; } = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";

    private static readonly Lazy<AppConfig> Instance2 = new(LoadConfig);

    public static AppConfig Instance => Instance2.Value;

    /// <summary>
    /// Event raised when an error occurs during configuration loading.
    /// Subscribers can report the exception to the bug reporting service.
    /// </summary>
    public static event EventHandler<Exception>? ConfigLoadError;

    private static AppConfig LoadConfig()
    {
        try
        {
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "appsettings.json");

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            // Report the exception via event to allow bug reporting
            ConfigLoadError?.Invoke(null, ex);
        }

        return new AppConfig();
    }
}