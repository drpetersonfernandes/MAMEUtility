using System.Diagnostics;
using MAMEUtility.Commands;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility.ViewModels;

/// <inheritdoc />
/// <summary>
/// ViewModel for the AboutWindow
/// </summary>
public class AboutViewModel : BaseViewModel
{
    private readonly ILogService _logService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logService">Log service</param>
    public AboutViewModel(ILogService logService)
    {
        _logService = logService;

        CloseCommand = new RelayCommand(Close);
        NavigateCommand = new RelayCommand<string>(Navigate);
    }

    /// <summary>
    /// Gets the application version
    /// </summary>
    public string ApplicationVersion => AboutWindow.ApplicationVersion;

    /// <summary>
    /// Gets the command to close the window
    /// </summary>
    public RelayCommand CloseCommand { get; }

    /// <summary>
    /// Gets the command to navigate to a URL
    /// </summary>
    public RelayCommand<string> NavigateCommand { get; }

    /// <summary>
    /// Event raised when the window should be closed
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Closes the window
    /// </summary>
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Navigates to a URL
    /// </summary>
    /// <param name="url">URL to navigate to</param>
    private void Navigate(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logService.LogError($"Unable to open the link: {ex.Message}");
        }
    }
}