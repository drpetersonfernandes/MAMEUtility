using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using MAMEUtility.Interfaces;

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
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.IO.IOException)
        {
            _logService.LogWarning($"Unable to open the link ({e.Uri.AbsoluteUri}): {ex.Message}");
        }
        catch (Exception ex)
        {
            _logService.LogError($"Unable to open the link: {ex.Message}");
        }

        e.Handled = true;
    }
}