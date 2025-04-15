using MAMEUtility.Commands;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility.ViewModels;

public class LogViewModel : BaseViewModel
{
    private readonly ILogService _logService;
    private string _logText = string.Empty;

    public LogViewModel(ILogService logService)
    {
        _logService = logService;
        _logService.LogMessageAdded += OnLogMessageAdded;

        ClearCommand = new RelayCommand(Clear);
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public RelayCommand ClearCommand { get; }

    public void AppendLog(string message)
    {
        LogText += message + "\n";
    }

    private void Clear()
    {
        LogText = string.Empty;
    }

    private void OnLogMessageAdded(object? sender, string message)
    {
        AppendLog(message);
    }
}