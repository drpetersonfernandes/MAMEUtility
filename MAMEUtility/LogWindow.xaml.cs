using MAMEUtility.ViewModels;

namespace MAMEUtility;

/// <inheritdoc cref="System.Windows.Window" />
/// <summary>
/// Interaction logic for LogWindow.xaml
/// </summary>
public partial class LogWindow
{
    /// <inheritdoc />
    /// <summary>
    /// Constructor
    /// </summary>
    public LogWindow()
    {
        InitializeComponent();

        // Set the data context
        DataContext = ServiceLocator.Instance.Resolve<LogViewModel>();
    }

    /// <summary>
    /// Appends a log message to the log window
    /// </summary>
    /// <param name="message">Message to append</param>
    public virtual void AppendLog(string message)
    {
        // This method is called by the legacy code
        // In our MVVM implementation, we'll route it to the ViewModel
        if (DataContext is LogViewModel viewModel)
        {
            viewModel.AppendLog(message);
        }

        // Make sure the text box is scrolled to the end
        LogTextBox.ScrollToEnd();
    }
}