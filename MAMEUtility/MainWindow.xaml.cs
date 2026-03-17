using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public partial class MainWindow
{
    private readonly ILogService _logService;
    private readonly IDialogService _dialogService;
    private readonly IMameProcessingService _mameProcessingService;
    private readonly IApplicationStatsService _appStatsService;
    private readonly IVersionCheckService _versionCheckService;
    private readonly DispatcherTimer _processingTimer;
    private readonly Stopwatch _processingStopwatch;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();

        // Get services from the locator
        var serviceLocator = ServiceLocator.Instance;
        _logService = serviceLocator.Resolve<ILogService>();
        _dialogService = serviceLocator.Resolve<IDialogService>();
        _mameProcessingService = serviceLocator.Resolve<IMameProcessingService>();
        _appStatsService = serviceLocator.Resolve<IApplicationStatsService>();
        _versionCheckService = serviceLocator.Resolve<IVersionCheckService>();

        // Initialize timer for processing time
        _processingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _processingTimer.Tick += ProcessingTimer_Tick;

        _processingStopwatch = new Stopwatch();

        // Subscribe to log messages
        _logService.LogMessageAdded += LogService_LogMessageAdded;
        _logService.BatchOperationCompleted += LogService_BatchOperationCompleted;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private static void LogService_BatchOperationCompleted(object? sender, (string Title, string Message, bool HasErrors) e)
    {
        MessageBox.Show(e.Message, e.Title, MessageBoxButton.OK, e.HasErrors ? MessageBoxImage.Error : MessageBoxImage.Warning);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await HandleStartupServicesAsync();
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in MainWindow_Loaded");
        }
    }

    private async Task HandleStartupServicesAsync()
    {
        // 1. Call application stats API
        await _appStatsService.SendStartStatsAsync();

        // 2. Check for updates
        var (isNewVersionAvailable, latestVersion, downloadUrl) = await _versionCheckService.CheckForUpdatesAsync();
        if (isNewVersionAvailable && !string.IsNullOrEmpty(latestVersion) && !string.IsNullOrEmpty(downloadUrl))
        {
            _logService.Log($"A new version is available: {latestVersion}");

            var result = MessageBox.Show(
                $"A new version ({latestVersion}) of MAME Utility is available.\nWould you like to visit the download page?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = downloadUrl,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Failed to open download URL: {ex.Message}");
                }
            }
        }
        else
        {
            _logService.Log("You are using the latest version.");
        }
    }

    private void LogService_LogMessageAdded(object? sender, string message)
    {
        // LogService already dispatches to UI thread via Dispatcher.BeginInvoke,
        // so this handler is always called on the UI thread.
        LogViewer.AppendText(message);
        LogViewer.ScrollToEnd();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // Unsubscribe from events to prevent memory leak
        _logService.LogMessageAdded -= LogService_LogMessageAdded;
        _logService.BatchOperationCompleted -= LogService_BatchOperationCompleted;
    }

    public static string VersionText => ServiceLocator.Instance.Resolve<IVersionService>().ApplicationVersion;

    private void ProcessingTimer_Tick(object? sender, EventArgs e)
    {
        UpdateProgressText();
    }

    private void UpdateProgressText()
    {
        var elapsed = _processingStopwatch.Elapsed;
        var progress = OverallProgressBar.Value;
        ProgressPercentageTextBlock.Text = $"{progress:0}% | Elapsed: {elapsed:mm\\:ss}";
    }

    private void SetProcessingState(bool isProcessing, string? operationName = null)
    {
        StatusBarText.Text = isProcessing ? $"{operationName} in progress..." : "Ready";
        CancelProcessingButton.Visibility = isProcessing ? Visibility.Visible : Visibility.Collapsed;
        OverallProgressBar.IsIndeterminate = isProcessing && OverallProgressBar.Value <= 0;

        if (isProcessing)
        {
            ProcessingTextBlock.Text = !string.IsNullOrEmpty(operationName)
                ? $"Processing {operationName}..."
                : "Processing...";

            _cancellationTokenSource = new CancellationTokenSource();
            _processingStopwatch.Restart();
            _processingTimer.Start();
        }
        else
        {
            ProcessingTextBlock.Text = "Ready";
            ProgressPercentageTextBlock.Text = OverallProgressBar.Value >= 100 ? "Completed | " + $"Elapsed: {_processingStopwatch.Elapsed:mm\\:ss}" : "Ready";
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _processingStopwatch.Stop();
            _processingTimer.Stop();
        }

        // Disable/Enable all operational buttons
        var buttons = new[]
        {
            CreateMameFullButton, CreateMameManufacturerButton, CreateMameYearButton,
            CreateMameSourcefileButton, CreateMameSoftwareListButton, MergeListsButton,
            StartCopyRomsButton, StartCopyImagesButton,
            // Browse buttons
            BrowseCopyRomsSourceButton, BrowseCopyRomsDestButton, BrowseCopyRomsXmlButton,
            BrowseCopyImagesSourceButton, BrowseCopyImagesDestButton, BrowseCopyImagesXmlButton
        };

        foreach (var button in buttons)
        {
            button.IsEnabled = !isProcessing;
        }

        // Disable/Enable text boxes to prevent path changes during processing
        var textBoxes = new[]
        {
            CopyRomsSourceTextBox, CopyRomsDestTextBox, CopyRomsXmlTextBox,
            CopyImagesSourceTextBox, CopyImagesDestTextBox, CopyImagesXmlTextBox
        };

        foreach (var textBox in textBoxes)
        {
            textBox.IsEnabled = !isProcessing;
        }

        if (isProcessing)
        {
            OverallProgressBar.Value = 0;
            UpdateProgressText();
        }
    }

    private void CancelProcessingButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _logService.Log("Cancellation requested by user...");
        }
        catch (Exception ex)
        {
            _logService.LogError($"Error cancelling operation: {ex.Message}");
        }
    }

    private async void CreateMameFull_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "MAME Full List");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token; // This line was missing

                _logService.Log("Select MAME full driver information in XML. You can download this file from the MAME Website.");
                var inputFilePaths = _dialogService.ShowOpenFileDialog(
                    "Select MAME full driver information in XML",
                    "XML files (*.xml)|*.xml");

                if (inputFilePaths == null || inputFilePaths.Length == 0)
                {
                    _logService.Log("No input file selected. Operation cancelled.");
                    return;
                }

                var inputFilePath = inputFilePaths[0];

                _logService.Log("Put a name to your output file.");
                var outputFilePath = _dialogService.ShowSaveFileDialog(
                    "Save MAMEFull",
                    "XML files (*.xml)|*.xml",
                    "MAMEFull.xml");

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    _logService.Log("No output file specified for MAMEFull.xml. Operation cancelled.");
                    return;
                }

                if (FileNameHelper.ArePathsEqual(inputFilePath, outputFilePath))
                {
                    _logService.LogError("Input and output files cannot be the same. Please choose a different name or location for the output file.");
                    return;
                }

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.CreateMameFullListAsync(inputFilePath, outputFilePath, progress, token);
                _logService.Log("Output file saved.");
            }
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to create MAME Full list. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void CreateMameManufacturer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "Manufacturer Lists");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token; // Add this line

                _logService.Log("Select MAME full driver information in XML. You can download this file from the MAME Website.");
                var inputFilePaths = _dialogService.ShowOpenFileDialog(
                    "Select MAME full driver information in XML",
                    "XML files (*.xml)|*.xml");

                if (inputFilePaths == null || inputFilePaths.Length == 0)
                {
                    _logService.Log("No input file selected. Operation cancelled.");
                    return;
                }

                var inputFilePath = inputFilePaths[0];

                _logService.Log("Select Output Folder.");
                var outputFolderPath = _dialogService.ShowFolderBrowserDialog("Select Output Folder");

                if (string.IsNullOrEmpty(outputFolderPath))
                {
                    _logService.Log("No output folder specified. Operation cancelled.");
                    return;
                }

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.CreateMameManufacturerListsAsync(inputFilePath, outputFolderPath, progress, token);
                _logService.Log("Data extracted and saved successfully for all manufacturers.");
            }
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to create manufacturer lists. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void CreateMameYear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "Year Lists");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token; // Add this line

                _logService.Log("Select MAME full driver information in XML. You can download this file from the MAME Website.");
                var inputFilePaths = _dialogService.ShowOpenFileDialog(
                    "Select MAME full driver information in XML",
                    "XML files (*.xml)|*.xml");

                if (inputFilePaths == null || inputFilePaths.Length == 0)
                {
                    _logService.Log("No input file selected. Operation cancelled.");
                    return;
                }

                var inputFilePath = inputFilePaths[0];

                _logService.Log("Select Output Folder.");
                var outputFolderPath = _dialogService.ShowFolderBrowserDialog("Select Output Folder");

                if (string.IsNullOrEmpty(outputFolderPath))
                {
                    _logService.Log("No output folder specified. Operation cancelled.");
                    return;
                }

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.CreateMameYearListsAsync(inputFilePath, outputFolderPath, progress, token);
                _logService.Log("XML files created successfully for all years.");
            }
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to create year lists. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void CreateMameSourcefile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "Sourcefile Lists");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token; // Add this line

                _logService.Log("Select MAME full driver information in XML. You can download this file from the MAME Website.");
                var inputFilePaths = _dialogService.ShowOpenFileDialog(
                    "Select MAME full driver information in XML",
                    "XML files (*.xml)|*.xml");

                if (inputFilePaths == null || inputFilePaths.Length == 0)
                {
                    _logService.Log("No input file selected. Operation cancelled.");
                    return;
                }

                var inputFilePath = inputFilePaths[0];

                _logService.Log("Select Output Folder.");
                var outputFolderPath = _dialogService.ShowFolderBrowserDialog("Select Output Folder");

                if (string.IsNullOrEmpty(outputFolderPath))
                {
                    _logService.Log("No output folder specified. Operation cancelled.");
                    return;
                }

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.CreateMameSourcefileListsAsync(inputFilePath, outputFolderPath, progress, token);
                _logService.Log("Data extracted and saved successfully for all source files.");
            }
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to create sourcefile lists. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void CreateMameSoftwareList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "Software List");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token; // Add this line

                _logService.Log("Select the folder containing XML files to process.");
                var inputFolderPath = _dialogService.ShowFolderBrowserDialog("Select the folder containing XML files to process");

                if (string.IsNullOrEmpty(inputFolderPath))
                {
                    _logService.Log("No folder selected. Operation cancelled.");
                    return;
                }

                _logService.Log("Choose a location to save the consolidated output XML file.");
                var outputFilePath = _dialogService.ShowSaveFileDialog(
                    "Save Consolidated XML File",
                    "XML Files (*.xml)|*.xml",
                    "MAMESoftwareList.xml");

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    _logService.Log("No output file specified. Operation cancelled.");
                    return;
                }

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.CreateMameSoftwareListAsync(inputFolderPath, outputFilePath, progress, token);
                _logService.Log("Consolidated XML file created successfully.");
            }
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to create software list. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void MergeLists_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "List Merging");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token;

                _logService.Log("Select XML files to merge. You can select multiple XML files.");
                var inputFilePaths = _dialogService.ShowOpenFileDialog(
                    "Select XML files to merge",
                    "XML files (*.xml)|*.xml",
                    true);

                if (inputFilePaths == null || inputFilePaths.Length == 0)
                {
                    _logService.Log("No input file selected. Operation cancelled.");
                    return;
                }

                _logService.Log("Select where to save the merged XML file.");
                var outputXmlPath = _dialogService.ShowSaveFileDialog(
                    "Save Merged XML",
                    "XML files (*.xml)|*.xml",
                    "Merged.xml");

                if (string.IsNullOrEmpty(outputXmlPath))
                {
                    _logService.Log("No output file specified for merged XML. Operation cancelled.");
                    return;
                }

                var outputDatPath = Path.ChangeExtension(outputXmlPath, ".dat");

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.MergeListsAsync(inputFilePaths, outputXmlPath, outputDatPath, progress, token);
                _logService.Log($"Merging completed. Created XML file ({outputXmlPath}) and DAT file ({outputDatPath}).");
            }
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to merge lists. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void StartCopyRoms_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sourceDirectory = CopyRomsSourceTextBox.Text;
            var destinationDirectory = CopyRomsDestTextBox.Text;
            var xmlFilePaths = CopyRomsXmlTextBox.Text.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(destinationDirectory) || xmlFilePaths.Length == 0)
            {
                _dialogService.ShowError("Please specify Source, Destination and XML files.");
                return;
            }

            SetProcessingState(true, "ROM Copy");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token;

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.CopyRomsAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, token);
            }

            _logService.Log("ROM copy operation is finished.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to copy ROMs. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void StartCopyImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sourceDirectory = CopyImagesSourceTextBox.Text;
            var destinationDirectory = CopyImagesDestTextBox.Text;
            var xmlFilePaths = CopyImagesXmlTextBox.Text.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(destinationDirectory) || xmlFilePaths.Length == 0)
            {
                _dialogService.ShowError("Please specify Source, Destination and XML files.");
                return;
            }

            SetProcessingState(true, "Image Copy");

            if (_cancellationTokenSource != null)
            {
                var token = _cancellationTokenSource.Token;

                var progress = new Progress<int>(value =>
                {
                    OverallProgressBar.Value = value;
                });

                await _mameProcessingService.CopyImagesAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, token);
            }

            _logService.Log("Image copy operation is finished.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception)
        {
            // Exception already logged by MameProcessingService - just show user feedback
            _logService.LogError("Failed to copy images. See log for details.");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void Donate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://www.purelogiccode.com/donate",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError("Unable to open the link: " + ex.Message);
            await _logService.LogExceptionAsync(ex, "Error in Donate");
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        _dialogService.ShowAboutWindow();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    #region Browse Handlers

    private void BrowseCopyRomsSource_Click(object sender, RoutedEventArgs e)
    {
        var folder = _dialogService.ShowFolderBrowserDialog("Select Source ROMs Directory");
        if (!string.IsNullOrEmpty(folder))
        {
            CopyRomsSourceTextBox.Text = folder;
        }
    }

    private void BrowseCopyRomsDest_Click(object sender, RoutedEventArgs e)
    {
        var folder = _dialogService.ShowFolderBrowserDialog("Select Destination ROMs Directory");
        if (!string.IsNullOrEmpty(folder))
        {
            CopyRomsDestTextBox.Text = folder;
        }
    }

    private void BrowseCopyRomsXml_Click(object sender, RoutedEventArgs e)
    {
        var files = _dialogService.ShowOpenFileDialog("Select XML Selection File(s)", "XML files (*.xml)|*.xml", true);
        if (files is { Length: > 0 })
        {
            CopyRomsXmlTextBox.Text = string.Join("|", files);
        }
    }

    private void BrowseCopyImagesSource_Click(object sender, RoutedEventArgs e)
    {
        var folder = _dialogService.ShowFolderBrowserDialog("Select Source Images Directory");
        if (!string.IsNullOrEmpty(folder))
        {
            CopyImagesSourceTextBox.Text = folder;
        }
    }

    private void BrowseCopyImagesDest_Click(object sender, RoutedEventArgs e)
    {
        var folder = _dialogService.ShowFolderBrowserDialog("Select Destination Images Directory");
        if (!string.IsNullOrEmpty(folder))
        {
            CopyImagesDestTextBox.Text = folder;
        }
    }

    private void BrowseCopyImagesXml_Click(object sender, RoutedEventArgs e)
    {
        var files = _dialogService.ShowOpenFileDialog("Select XML Selection File(s)", "XML files (*.xml)|*.xml", true);
        if (files is { Length: > 0 })
        {
            CopyImagesXmlTextBox.Text = string.Join("|", files);
        }
    }

    #endregion
}
