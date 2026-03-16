namespace MAMEUtility.Interfaces;

public interface IVersionCheckService
{
    Task<(bool IsNewVersionAvailable, string? LatestVersion, string? DownloadUrl)> CheckForUpdatesAsync();
}
