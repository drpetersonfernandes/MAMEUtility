using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow
{
    private readonly ILogService _logService;

    /// <summary>
    /// Constructor
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();

        // Get the log service
        _logService = ServiceLocator.Instance.Resolve<ILogService>();

        // Set the version text
        VersionTextBlock.Text = ApplicationVersion;
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logService.LogError($"Unable to open the link: {ex.Message}");
        }

        e.Handled = true;
    }
}