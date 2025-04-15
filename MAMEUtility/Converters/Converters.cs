using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MAMEUtility.Converters;

/// <inheritdoc />
/// <summary>
/// Converter that converts a boolean value to a Visibility value
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    /// <summary>
    /// Converts a boolean value to a Visibility value
    /// </summary>
    /// <param name="value">Boolean value to convert</param>
    /// <param name="targetType">Target type</param>
    /// <param name="parameter">Converter parameter</param>
    /// <param name="culture">Culture info</param>
    /// <returns>Visibility.Visible if the boolean value is true, otherwise Visibility.Collapsed</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    /// <summary>
    /// Converts a Visibility value to a boolean value
    /// </summary>
    /// <param name="value">Visibility value to convert</param>
    /// <param name="targetType">Target type</param>
    /// <param name="parameter">Converter parameter</param>
    /// <param name="culture">Culture info</param>
    /// <returns>True if the Visibility value is Visibility.Visible, otherwise false</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}

/// <inheritdoc />
/// <summary>
/// Converter that converts a progress value to a width value
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    /// <inheritdoc />
    /// <summary>
    /// Converts a progress value to a width value
    /// </summary>
    /// <param name="value">Progress value to convert</param>
    /// <param name="targetType">Target type</param>
    /// <param name="parameter">Converter parameter</param>
    /// <param name="culture">Culture info</param>
    /// <returns>Width as a percentage of the progress value</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double progressValue) return "0%";
        // Ensures progress is between 0 and 100
        progressValue = Math.Max(0, Math.Min(100, progressValue));
        return progressValue + "%";
    }

    /// <inheritdoc />
    /// <summary>
    /// Not implemented
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}