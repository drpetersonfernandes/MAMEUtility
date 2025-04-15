namespace MAMEUtility.Services.Interfaces;

public interface IBugReportService : IDisposable
{
    Task SendExceptionReportAsync(Exception exception);
}