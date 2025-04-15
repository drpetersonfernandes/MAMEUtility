﻿using System.Diagnostics;
using System.IO;
using MAMEUtility.Commands;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility.ViewModels;

/// <inheritdoc />
/// <summary>
/// ViewModel for the MainWindow
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly ILogService _logService;
    private readonly IDialogService _dialogService;
    private readonly IMameProcessingService _mameProcessingService;
    private int _overallProgress;
    private bool _isProcessing;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logService">Log service</param>
    /// <param name="dialogService">Dialog service</param>
    /// <param name="mameProcessingService">MAME processing service</param>
    public MainViewModel(
        ILogService logService,
        IDialogService dialogService,
        IMameProcessingService mameProcessingService)
    {
        _logService = logService;
        _dialogService = dialogService;
        _mameProcessingService = mameProcessingService;

        // Initialize commands
        CreateMameFullCommand = new RelayCommand(CreateMameFull, CanExecuteCommand);
        CreateMameManufacturerCommand = new RelayCommand(CreateMameManufacturer, CanExecuteCommand);
        CreateMameYearCommand = new RelayCommand(CreateMameYear, CanExecuteCommand);
        CreateMameSourcefileCommand = new RelayCommand(CreateMameSourcefile, CanExecuteCommand);
        CreateMameSoftwareListCommand = new RelayCommand(CreateMameSoftwareList, CanExecuteCommand);
        MergeListsCommand = new RelayCommand(MergeLists, CanExecuteCommand);
        CopyRomsCommand = new RelayCommand(CopyRoms, CanExecuteCommand);
        CopyImagesCommand = new RelayCommand(CopyImages, CanExecuteCommand);
        DonateCommand = new RelayCommand(Donate);
        AboutCommand = new RelayCommand(ShowAbout);
        ExitCommand = new RelayCommand(Exit);
        ShowLogCommand = new RelayCommand(ShowLog);
    }

    /// <summary>
    /// Gets or sets the overall progress
    /// </summary>
    public int OverallProgress
    {
        get => _overallProgress;
        set => SetProperty(ref _overallProgress, value);
    }

    /// <summary>
    /// Gets or sets whether an operation is in progress
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (!SetProperty(ref _isProcessing, value)) return;
            // Raise property changed for all commands that depend on IsProcessing
            OnPropertyChanged(nameof(CreateMameFullCommand));
            OnPropertyChanged(nameof(CreateMameManufacturerCommand));
            OnPropertyChanged(nameof(CreateMameYearCommand));
            OnPropertyChanged(nameof(CreateMameSourcefileCommand));
            OnPropertyChanged(nameof(CreateMameSoftwareListCommand));
            OnPropertyChanged(nameof(MergeListsCommand));
            OnPropertyChanged(nameof(CopyRomsCommand));
            OnPropertyChanged(nameof(CopyImagesCommand));
        }
    }

    /// <summary>
    /// Gets the command to create a MAME full list
    /// </summary>
    public RelayCommand CreateMameFullCommand { get; }

    /// <summary>
    /// Gets the command to create MAME manufacturer lists
    /// </summary>
    public RelayCommand CreateMameManufacturerCommand { get; }

    /// <summary>
    /// Gets the command to create MAME year lists
    /// </summary>
    public RelayCommand CreateMameYearCommand { get; }

    /// <summary>
    /// Gets the command to create MAME sourcefile lists
    /// </summary>
    public RelayCommand CreateMameSourcefileCommand { get; }

    /// <summary>
    /// Gets the command to create a MAME software list
    /// </summary>
    public RelayCommand CreateMameSoftwareListCommand { get; }

    /// <summary>
    /// Gets the command to merge lists
    /// </summary>
    public RelayCommand MergeListsCommand { get; }

    /// <summary>
    /// Gets the command to copy ROMs
    /// </summary>
    public RelayCommand CopyRomsCommand { get; }

    /// <summary>
    /// Gets the command to copy images
    /// </summary>
    public RelayCommand CopyImagesCommand { get; }

    /// <summary>
    /// Gets the command to show the donate page
    /// </summary>
    public RelayCommand DonateCommand { get; }

    /// <summary>
    /// Gets the command to show the about window
    /// </summary>
    public RelayCommand AboutCommand { get; }

    /// <summary>
    /// Gets the command to exit the application
    /// </summary>
    public RelayCommand ExitCommand { get; }

    /// <summary>
    /// Gets the command to show the log window
    /// </summary>
    public RelayCommand ShowLogCommand { get; }

    /// <summary>
    /// Creates a MAME full list
    /// </summary>
    private async void CreateMameFull()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

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

            var progress = new Progress<int>(value => { OverallProgress = value; });

            await _mameProcessingService.CreateMameFullListAsync(inputFilePath, outputFilePath, progress);

            _logService.Log("Output file saved.");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameFull");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Creates MAME manufacturer lists
    /// </summary>
    private async void CreateMameManufacturer()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

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

            var progress = new Progress<int>(value => { OverallProgress = value; });

            await _mameProcessingService.CreateMameManufacturerListsAsync(inputFilePath, outputFolderPath, progress);

            _logService.Log("Data extracted and saved successfully for all manufacturers.");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameManufacturer");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Creates MAME year lists
    /// </summary>
    private async void CreateMameYear()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

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

            var progress = new Progress<int>(value => { OverallProgress = value; });

            await _mameProcessingService.CreateMameYearListsAsync(inputFilePath, outputFolderPath, progress);

            _logService.Log("XML files created successfully for all years.");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameYear");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Creates MAME sourcefile lists
    /// </summary>
    private async void CreateMameSourcefile()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

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

            var progress = new Progress<int>(value => { OverallProgress = value; });

            await _mameProcessingService.CreateMameSourcefileListsAsync(inputFilePath, outputFolderPath, progress);

            _logService.Log("Data extracted and saved successfully for all source files.");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameSourcefile");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Creates a MAME software list
    /// </summary>
    private async void CreateMameSoftwareList()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

            _logService.Log("Select the folder containing XML files to process.");
            var inputFolderPath = _dialogService.ShowFolderBrowserDialog(
                "Select the folder containing XML files to process");

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

            var progress = new Progress<int>(value => { OverallProgress = value; });

            await _mameProcessingService.CreateMameSoftwareListAsync(inputFolderPath, outputFilePath, progress);

            _logService.Log("Consolidated XML file created successfully.");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CreateMameSoftwareList");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Merges lists
    /// </summary>
    private async void MergeLists()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

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

            // Create DAT filename based on XML filename (replace extension)
            var outputDatPath = Path.ChangeExtension(outputXmlPath, ".dat");

            await _mameProcessingService.MergeListsAsync(inputFilePaths, outputXmlPath, outputDatPath);

            _logService.Log($"Merging completed. Created XML file ({outputXmlPath}) and DAT file ({outputDatPath}).");

            OverallProgress = 100;
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in MergeLists");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Copies ROMs
    /// </summary>
    private async void CopyRoms()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

            _logService.Log("Select the source directory containing the ROMs.");
            var sourceDirectory = _dialogService.ShowFolderBrowserDialog(
                "Select the source directory containing the ROMs");

            if (string.IsNullOrEmpty(sourceDirectory))
            {
                _logService.Log("You did not provide the source directory containing the ROMs. Operation cancelled.");
                return;
            }

            _logService.Log("Select the destination directory for the ROMs.");
            var destinationDirectory = _dialogService.ShowFolderBrowserDialog(
                "Select the destination directory for the ROMs");

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

            var progress = new Progress<int>(value => { OverallProgress = value; });

            await _mameProcessingService.CopyRomsAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress);

            _logService.Log("ROM copy operation is finished.");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CopyRoms");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Copies images
    /// </summary>
    private async void CopyImages()
    {
        try
        {
            OverallProgress = 0;
            IsProcessing = true;

            _logService.Log("Select the source directory containing the images.");
            var sourceDirectory = _dialogService.ShowFolderBrowserDialog(
                "Select the source directory containing the images");

            if (string.IsNullOrEmpty(sourceDirectory))
            {
                _logService.Log("No source directory selected. Operation cancelled.");
                return;
            }

            _logService.Log("Select the destination directory for the images.");
            var destinationDirectory = _dialogService.ShowFolderBrowserDialog(
                "Select the destination directory for the images");

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

            var progress = new Progress<int>(value => { OverallProgress = value; });

            await _mameProcessingService.CopyImagesAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress);

            _logService.Log("Image copy operation is finished.");
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, "Error in CopyImages");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Opens the donate page
    /// </summary>
    private void Donate()
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

    /// <summary>
    /// Shows the about window
    /// </summary>
    private void ShowAbout()
    {
        _dialogService.ShowAboutWindow();
    }

    /// <summary>
    /// Shows the log window
    /// </summary>
    private void ShowLog()
    {
        _logService.ShowLogWindow();
    }

    /// <summary>
    /// Exits the application
    /// </summary>
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// Determines whether a command can be executed
    /// </summary>
    /// <returns>True if the command can be executed, otherwise false</returns>
    private bool CanExecuteCommand()
    {
        return !IsProcessing;
    }
}