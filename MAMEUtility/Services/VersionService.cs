using System.Reflection;
using MAMEUtility.Interfaces;

namespace MAMEUtility.Services;

/// <summary>
/// Service for retrieving application version information.
/// Centralizes version retrieval to avoid dependencies on UI classes.
/// </summary>
public class VersionService : IVersionService
{
    private readonly Lazy<string> _rawVersion;
    private readonly Lazy<string> _applicationVersion;

    public VersionService()
    {
        _rawVersion = new Lazy<string>(GetRawVersion);
        _applicationVersion = new Lazy<string>(() => "Version: " + _rawVersion.Value);
    }

    /// <inheritdoc />
    public string ApplicationVersion => _applicationVersion.Value;

    /// <inheritdoc />
    public string RawVersion => _rawVersion.Value;

    private static string GetRawVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "0.0.0.0";
    }
}
