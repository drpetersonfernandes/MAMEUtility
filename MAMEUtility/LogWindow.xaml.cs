using MAMEUtility.ViewModels;

namespace MAMEUtility;

public partial class LogWindow
{
    public LogWindow()
    {
        InitializeComponent();

        // Set the data context
        DataContext = ServiceLocator.Instance.Resolve<LogViewModel>();
    }

    public void AppendLog(string message)
    {
        if (DataContext is LogViewModel viewModel)
        {
            viewModel.AppendLog(message);
        }

        // Make sure the text box is scrolled to the end
        LogTextBox.ScrollToEnd();
    }
}