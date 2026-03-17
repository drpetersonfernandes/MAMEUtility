using System.Diagnostics;
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
    private readonly IVersionService _versionService;

    /// <summary>
    /// Constructor
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();

        // Get services
        _logService = ServiceLocator.Instance.Resolve<ILogService>();
        _versionService = ServiceLocator.Instance.Resolve<IVersionService>();

        // Set the version text
        VersionTextBlock.Text = _versionService.ApplicationVersion;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            if (e.Uri == null) return;

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