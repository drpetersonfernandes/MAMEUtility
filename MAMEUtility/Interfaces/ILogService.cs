namespace MAMEUtility.Interfaces;

public interface ILogService
{
    void Log(string message);

    void LogError(string message);

    void LogWarning(string message);

    Task LogExceptionAsync(Exception exception, string additionalInfo = "");

    void BeginBatchOperation();
    void EndBatchOperation(string summaryTitle = "Batch Operation Completed");

    event EventHandler<string> LogMessageAdded;
    event EventHandler<(string Title, string Message, bool HasErrors)> BatchOperationCompleted;
}