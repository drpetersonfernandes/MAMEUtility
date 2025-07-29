using System.Windows;
using System.Windows.Controls;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

public partial class LogWindow
{
    private readonly ILogService _logService;

    public LogWindow()
    {
        InitializeComponent();

        _logService = ServiceLocator.Instance.Resolve<ILogService>();
        _logService.LogMessageAdded += OnLogMessageAdded;

        // Add TextChanged event handler for auto-scrolling
        LogTextBox.TextChanged += LogTextBox_TextChanged;
    }

    private void OnLogMessageAdded(object? sender, string message)
    {
        // The LogService ensures this is called on the UI thread.
        LogTextBox.AppendText(message + Environment.NewLine);
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }

    // Event handler to scroll to the end when text changes
    private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LogTextBox.ScrollToEnd();
    }

    // Unsubscribe from event when window closes to prevent potential memory leaks
    protected override void OnClosed(EventArgs e)
    {
        _logService.LogMessageAdded -= OnLogMessageAdded;
        LogTextBox.TextChanged -= LogTextBox_TextChanged;
        base.OnClosed(e);
    }
}