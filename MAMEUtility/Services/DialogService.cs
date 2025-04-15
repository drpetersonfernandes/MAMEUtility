using System.Windows;
using MAMEUtility.Services.Interfaces;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MAMEUtility.Services;

/// <inheritdoc />
/// <summary>
/// Implementation of the IDialogService interface
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc />
    /// <summary>
    /// Shows a message to the user
    /// </summary>
    /// <param name="message">Message to show</param>
    /// <param name="title">Dialog title</param>
    public void ShowMessage(string message, string title = "Information")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows an error message to the user
    /// </summary>
    /// <param name="message">Error message to show</param>
    /// <param name="title">Dialog title</param>
    public void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows a warning message to the user
    /// </summary>
    /// <param name="message">Warning message to show</param>
    /// <param name="title">Dialog title</param>
    public void ShowWarning(string message, string title = "Warning")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows a confirmation dialog to the user
    /// </summary>
    /// <param name="message">Message to show</param>
    /// <param name="title">Dialog title</param>
    /// <returns>True if the user confirmed, otherwise false</returns>
    public bool ShowConfirmation(string message, string title = "Confirmation")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows an open file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="filter">File filter</param>
    /// <param name="multiselect">Whether multiple files can be selected</param>
    /// <returns>Selected file paths, or null if dialog was cancelled</returns>
    public string[]? ShowOpenFileDialog(string title, string filter, bool multiselect = false)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = multiselect
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileNames;
        }

        return null;
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows a save file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="filter">File filter</param>
    /// <param name="defaultFileName">Default file name</param>
    /// <returns>Selected file path, or null if dialog was cancelled</returns>
    public string? ShowSaveFileDialog(string title, string filter, string defaultFileName = "")
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows a folder browser dialog
    /// </summary>
    /// <param name="description">Dialog description</param>
    /// <returns>Selected folder path, or null if dialog was cancelled</returns>
    public string? ShowFolderBrowserDialog(string description)
    {
        var dialog = new FolderBrowserDialog
        {
            Description = description
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            return dialog.SelectedPath;
        }

        return null;
    }

    /// <inheritdoc />
    /// <summary>
    /// Shows the About window
    /// </summary>
    public void ShowAboutWindow()
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog();
    }
}