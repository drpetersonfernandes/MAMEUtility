using System.IO;
using System.Text.RegularExpressions;

namespace MAMEUtility;

public static partial class FileNameHelper
{
    // Reserved Windows file names (case-insensitive)
    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Sanitizes a string to be safe for use as a file name.
    /// Replaces invalid characters, handles reserved names, and cleans whitespace.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <param name="defaultIfEmpty">The default string to return if the input becomes empty after sanitization.</param>
    /// <returns>A sanitized string suitable for a file name.</returns>
    public static string SanitizeForFileName(string input, string defaultIfEmpty = "Untitled")
    {
        if (string.IsNullOrWhiteSpace(input))
            return defaultIfEmpty;

        // 1. Replace invalid characters with underscore
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            input = input.Replace(invalidChar, '_');
        }

        // 2. Remove extra whitespace
        input = MyRegex().Replace(input, " ").Trim();

        // 3. Handle specific XML entity encoding that might appear in names
        // This is a common issue when parsing XML where '&' might be encoded as '&amp;'
        input = input.Replace("&amp;", "&");

        // 4. Check for reserved names (case-insensitive)
        if (ReservedWindowsNames.Contains(input.ToUpperInvariant()))
        {
            input += "_"; // Append an underscore to avoid conflict
        }

        // Ensure it's not empty after sanitization
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultIfEmpty;
        }

        return input;
    }

    /// <summary>
    /// Sanitizes a string for use as an XML element value.
    /// Primarily removes extra whitespace and handles common XML entities.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>A sanitized string suitable for an XML value.</returns>
    public static string SanitizeForXmlValue(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove extra whitespace
        input = MyRegex().Replace(input, " ").Trim();

        // Handle common XML entities that might be present in raw data
        input = input.Replace("&amp;", "&"); // Decode common XML entity if it's meant to be literal '&'

        return input;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}
