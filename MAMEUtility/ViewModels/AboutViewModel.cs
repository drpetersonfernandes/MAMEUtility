using System.Diagnostics;
using MAMEUtility.Commands;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility.ViewModels;

public class AboutViewModel : BaseViewModel
{
    private readonly ILogService _logService;

    public AboutViewModel(ILogService logService)
    {
        _logService = logService;

        CloseCommand = new RelayCommand(Close);
        NavigateCommand = new RelayCommand<string>(Navigate);
    }

    public string ApplicationVersion => AboutWindow.ApplicationVersion;

    public RelayCommand CloseCommand { get; }

    public RelayCommand<string> NavigateCommand { get; }

    public event EventHandler? CloseRequested;

    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

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