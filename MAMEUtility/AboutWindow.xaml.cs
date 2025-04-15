using System.Reflection;
using MAMEUtility.ViewModels;

namespace MAMEUtility;

/// <inheritdoc cref="System.Windows.Window" />
/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow
{
    /// <inheritdoc />
    /// <summary>
    /// Constructor
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();

        // Get the view model
        var viewModel = ServiceLocator.Instance.Resolve<AboutViewModel>();

        // Set the data context
        DataContext = viewModel;

        // Subscribe to close event
        viewModel.CloseRequested += (_, _) => Close();
    }

    /// <summary>
    /// Gets the application version
    /// </summary>
    public static string ApplicationVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return "Version: " + (version?.ToString() ?? "Unknown");
        }
    }
}