using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Application = System.Windows.Application;
using System.Windows.Threading;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public partial class MainWindow
{
    private readonly ILogService _logService;
    private readonly IDialogService _dialogService;
    private readonly IMameProcessingService _mameProcessingService;
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

        // Initialize timer for processing time
        _processingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _processingTimer.Tick += ProcessingTimer_Tick;

        _processingStopwatch = new Stopwatch();

        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        Application.Current.Shutdown();
    }

    public static string VersionText => AboutWindow.ApplicationVersion;

    private void ProcessingTimer_Tick(object? sender, EventArgs e)
    {
        ElapsedTimeTextBlock.Text = $"Elapsed: {_processingStopwatch.Elapsed:mm\\:ss}";
    }

    private void SetProcessingState(bool isProcessing, string? operationName = null)
    {
        ProcessingOverlay.Visibility = isProcessing ? Visibility.Visible : Visibility.Collapsed;
        StatusBarText.Text = isProcessing ? $"{operationName} in progress..." : "Ready";

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
            CopyRomsButton, CopyImagesButton
        };

        foreach (var button in buttons)
        {
            button.IsEnabled = !isProcessing;
        }

        if (isProcessing)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _processingStopwatch.Reset();
            _processingStopwatch.Start();
            _processingTimer.Start();
            ElapsedTimeTextBlock.Text = "Elapsed: 00:00";
            OverallProgressBar.Value = 0;
        }
        else
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _processingStopwatch.Stop();
            _processingTimer.Stop();
        }
    }

    private async void CreateMameFull_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "MAME Full List");

            var token = _cancellationTokenSource!.Token; // This line was missing

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

            var progress = new Progress<int>(value =>
            {
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.CreateMameFullListAsync(inputFilePath, outputFilePath, progress, token);
            _logService.Log("Output file saved.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameFull");
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

            var token = _cancellationTokenSource!.Token; // Add this line

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
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.CreateMameManufacturerListsAsync(inputFilePath, outputFolderPath, progress, token);
            _logService.Log("Data extracted and saved successfully for all manufacturers.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameManufacturer");
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

            var token = _cancellationTokenSource!.Token; // Add this line

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
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.CreateMameYearListsAsync(inputFilePath, outputFolderPath, progress, token);
            _logService.Log("XML files created successfully for all years.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameYear");
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

            var token = _cancellationTokenSource!.Token; // Add this line

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
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.CreateMameSourcefileListsAsync(inputFilePath, outputFolderPath, progress, token);
            _logService.Log("Data extracted and saved successfully for all source files.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameSourcefile");
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

            var token = _cancellationTokenSource!.Token; // Add this line

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
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.CreateMameSoftwareListAsync(inputFolderPath, outputFilePath, progress, token);
            _logService.Log("Consolidated XML file created successfully.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameSoftwareList");
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

            var token = _cancellationTokenSource!.Token;

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
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.MergeListsAsync(inputFilePaths, outputXmlPath, outputDatPath, progress, token);
            _logService.Log($"Merging completed. Created XML file ({outputXmlPath}) and DAT file ({outputDatPath}).");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in MergeLists");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void CopyRoms_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "ROM Copy");

            var token = _cancellationTokenSource!.Token; // Add this line

            _logService.Log("Select the source directory containing the ROMs.");
            var sourceDirectory = _dialogService.ShowFolderBrowserDialog("Select the source directory containing the ROMs");

            if (string.IsNullOrEmpty(sourceDirectory))
            {
                _logService.Log("You did not provide the source directory containing the ROMs. Operation cancelled.");
                return;
            }

            _logService.Log("Select the destination directory for the ROMs.");
            var destinationDirectory = _dialogService.ShowFolderBrowserDialog("Select the destination directory for the ROMs");

            if (string.IsNullOrEmpty(destinationDirectory))
            {
                _logService.Log("You did not select a destination directory for the ROMs. Operation cancelled.");
                return;
            }

            _logService.Log("Please select the XML file(s) containing ROM information. You can select multiple XML files.");
            var xmlFilePaths = _dialogService.ShowOpenFileDialog(
                "Please select the XML file(s) containing ROM information",
                "XML Files (*.xml)|*.xml",
                true);

            if (xmlFilePaths == null || xmlFilePaths.Length == 0)
            {
                _logService.Log("You did not provide the XML file(s) containing ROM information. Operation cancelled.");
                return;
            }

            var progress = new Progress<int>(value =>
            {
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.CopyRomsAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, token);
            _logService.Log("ROM copy operation is finished.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CopyRoms");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private async void CopyImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetProcessingState(true, "Image Copy");

            var token = _cancellationTokenSource!.Token;

            _logService.Log("Select the source directory containing the images.");
            var sourceDirectory = _dialogService.ShowFolderBrowserDialog("Select the source directory containing the images");

            if (string.IsNullOrEmpty(sourceDirectory))
            {
                _logService.Log("No source directory selected. Operation cancelled.");
                return;
            }

            _logService.Log("Select the destination directory for the images.");
            var destinationDirectory = _dialogService.ShowFolderBrowserDialog("Select the destination directory for the images");

            if (string.IsNullOrEmpty(destinationDirectory))
            {
                _logService.Log("No destination directory selected. Operation cancelled.");
                return;
            }

            _logService.Log("Please select the XML file(s) containing ROM information. You can select multiple XML files.");
            var xmlFilePaths = _dialogService.ShowOpenFileDialog(
                "Please select the XML file(s) containing ROM information",
                "XML Files (*.xml)|*.xml",
                true);

            if (xmlFilePaths == null || xmlFilePaths.Length == 0)
            {
                _logService.Log("No XML files selected. Operation cancelled.");
                return;
            }

            var progress = new Progress<int>(value =>
            {
                Dispatcher.BeginInvoke(() => OverallProgressBar.Value = value);
            });

            await _mameProcessingService.CopyImagesAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, token);
            _logService.Log("Image copy operation is finished.");
        }
        catch (OperationCanceledException)
        {
            _logService.Log("Operation was cancelled by user");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CopyImages");
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private void Donate_Click(object sender, RoutedEventArgs e)
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
            _ = _logService.LogExceptionAsync(ex, "Error in Donate");
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        _dialogService.ShowAboutWindow();
    }

    private void ShowLog_Click(object sender, RoutedEventArgs e)
    {
        _logService.ShowLogWindow();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void CancelProcessingButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _logService.Log("Operation cancellation requested by user");
        }
        catch (Exception ex)
        {
            _logService.LogError($"Failed to cancel operation: {ex.Message}");
        }
    }
}
