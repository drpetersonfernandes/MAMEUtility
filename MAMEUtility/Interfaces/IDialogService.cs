namespace MAMEUtility.Interfaces;

public interface IDialogService
{
    void ShowMessage(string message, string title = "Information");

    void ShowError(string message, string title = "Error");

    void ShowWarning(string message, string title = "Warning");

    bool ShowConfirmation(string message, string title = "Confirmation");

    string[]? ShowOpenFileDialog(string title, string filter, bool multiselect = false);

    string? ShowSaveFileDialog(string title, string filter, string defaultFileName = "");

    string? ShowFolderBrowserDialog(string description);

    void ShowAboutWindow();
}