namespace MAMEUtility.Services.Interfaces;

public interface ILogService
{
    void Log(string message);

    void LogError(string message);

    void LogWarning(string message);

    Task LogExceptionAsync(Exception exception, string additionalInfo = "");

    void ShowLogWindow();

    event EventHandler<string> LogMessageAdded;
}