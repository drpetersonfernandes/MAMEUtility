using MAMEUtility.ViewModels;
using System.Windows.Controls;

namespace MAMEUtility;

public partial class LogWindow
{
    public LogWindow()
    {
        InitializeComponent();

        // Set the data context
        var viewModel = ServiceLocator.Instance.Resolve<LogViewModel>();
        DataContext = viewModel;

        // Add TextChanged event handler for auto-scrolling
        LogTextBox.TextChanged += LogTextBox_TextChanged;
    }


    // Event handler to scroll to the end when text changes
    private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LogTextBox.ScrollToEnd();
    }

    // Unsubscribe from event when window closes to prevent potential memory leaks
    protected override void OnClosed(EventArgs e)
    {
        LogTextBox.TextChanged -= LogTextBox_TextChanged;
        base.OnClosed(e);
    }
}