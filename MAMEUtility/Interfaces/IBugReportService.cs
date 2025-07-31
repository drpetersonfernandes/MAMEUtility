namespace MAMEUtility.Interfaces;

public interface IBugReportService : IDisposable
{
    Task SendExceptionReportAsync(Exception exception);
}