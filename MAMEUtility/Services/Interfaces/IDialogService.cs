namespace MAMEUtility.Services.Interfaces;

/// <summary>
/// Service for showing dialogs to the user
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a message to the user
    /// </summary>
    /// <param name="message">Message to show</param>
    /// <param name="title">Dialog title</param>
    void ShowMessage(string message, string title = "Information");

    /// <summary>
    /// Shows an error message to the user
    /// </summary>
    /// <param name="message">Error message to show</param>
    /// <param name="title">Dialog title</param>
    void ShowError(string message, string title = "Error");

    /// <summary>
    /// Shows a warning message to the user
    /// </summary>
    /// <param name="message">Warning message to show</param>
    /// <param name="title">Dialog title</param>
    void ShowWarning(string message, string title = "Warning");

    /// <summary>
    /// Shows a confirmation dialog to the user
    /// </summary>
    /// <param name="message">Message to show</param>
    /// <param name="title">Dialog title</param>
    /// <returns>True if the user confirmed, otherwise false</returns>
    bool ShowConfirmation(string message, string title = "Confirmation");

    /// <summary>
    /// Shows an open file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="filter">File filter</param>
    /// <param name="multiselect">Whether multiple files can be selected</param>
    /// <returns>Selected file paths, or null if dialog was cancelled</returns>
    string[]? ShowOpenFileDialog(string title, string filter, bool multiselect = false);

    /// <summary>
    /// Shows a save file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="filter">File filter</param>
    /// <param name="defaultFileName">Default file name</param>
    /// <returns>Selected file path, or null if dialog was cancelled</returns>
    string? ShowSaveFileDialog(string title, string filter, string defaultFileName = "");

    /// <summary>
    /// Shows a folder browser dialog
    /// </summary>
    /// <param name="description">Dialog description</param>
    /// <returns>Selected folder path, or null if dialog was cancelled</returns>
    string? ShowFolderBrowserDialog(string description);

    /// <summary>
    /// Shows the About window
    /// </summary>
    void ShowAboutWindow();
}