using MAMEUtility.Commands;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility.ViewModels;

/// <inheritdoc />
/// <summary>
/// ViewModel for the LogWindow
/// </summary>
public class LogViewModel : BaseViewModel
{
    private readonly ILogService _logService;
    private string _logText = string.Empty;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logService">Log service</param>
    public LogViewModel(ILogService logService)
    {
        _logService = logService;
        _logService.LogMessageAdded += OnLogMessageAdded;

        ClearCommand = new RelayCommand(Clear);
    }

    /// <summary>
    /// Gets or sets the log text
    /// </summary>
    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    /// <summary>
    /// Gets the command to clear the log
    /// </summary>
    public RelayCommand ClearCommand { get; }

    /// <summary>
    /// Appends a message to the log
    /// </summary>
    /// <param name="message">Message to append</param>
    public void AppendLog(string message)
    {
        LogText += message + "\n";
    }

    /// <summary>
    /// Clears the log
    /// </summary>
    private void Clear()
    {
        LogText = string.Empty;
    }

    /// <summary>
    /// Handles the LogMessageAdded event
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="message">Log message</param>
    private void OnLogMessageAdded(object? sender, string message)
    {
        AppendLog(message);
    }
}