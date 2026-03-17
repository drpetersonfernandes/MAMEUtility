namespace MAMEUtility.Interfaces;

/// <summary>
/// Service for retrieving application version information.
/// Centralizes version retrieval to avoid dependencies on UI classes.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Gets the formatted application version string (e.g., "Version: 1.0.0.0").
    /// </summary>
    string ApplicationVersion { get; }

    /// <summary>
    /// Gets the raw application version string (e.g., "1.0.0.0").
    /// </summary>
    string RawVersion { get; }
}
