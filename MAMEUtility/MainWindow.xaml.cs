using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using MAMEUtility;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MameUtility;

public partial class MainWindow : INotifyPropertyChanged, IDisposable
{
    private readonly BackgroundWorker _worker;
    private readonly LogWindow _logWindow;
    private int _overallProgress;

    // Fix nullability to match interface exactly
    public event PropertyChangedEventHandler? PropertyChanged;

    // Add the missing OverallProgress property
    public int OverallProgress
    {
        get => _overallProgress;
        set
        {
            if (_overallProgress == value) return;

            _overallProgress = value;
            OnPropertyChanged(nameof(OverallProgress));
        }
    }

    // Method to raise PropertyChanged event with proper null checking
    // Removed 'virtual' since the class has no inheritors
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this; // Set DataContext explicitly to this

        _worker = new BackgroundWorker
        {
            WorkerReportsProgress = true
        };
        _worker.ProgressChanged += Worker_ProgressChanged;

        // Use the existing LogWindow from App instead of creating a new one
        _logWindow = App.SharedLogWindow ?? new LogWindow();

        if (App.SharedLogWindow != null) return;

        _logWindow.Show();
        App.SharedLogWindow = _logWindow;
    }

    private void DonateButton_Click(object sender, RoutedEventArgs e)
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
            MessageBox.Show("Unable to open the link: " + ex.Message);
            _ = LogError.LogAsync(ex, "Error in DonateButton_Click");
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        AboutWindow aboutWindow = new();
        aboutWindow.ShowDialog();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private async void CreateMAMEFull_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var inputFilePath = openFileDialog.FileName;

                _logWindow.AppendLog("Put a name to your output file.");
                SaveFileDialog saveFileDialog = new()
                {
                    Title = "Save MAMEFull",
                    Filter = "XML files (*.xml)|*.xml",
                    FileName = "MAMEFull.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var outputFilePathMameFull = saveFileDialog.FileName;

                    try
                    {
                        var inputDoc = XDocument.Load(inputFilePath);
                        await MameFull.CreateAndSaveMameFullAsync(inputDoc, outputFilePathMameFull, _worker, _logWindow);
                        _logWindow.AppendLog("Output file saved.");
                    }
                    catch (Exception ex)
                    {
                        _logWindow.AppendLog("An error occurred: " + ex.Message);
                        await LogError.LogAsync(ex, $"Error processing XML file: {inputFilePath}");
                    }
                }
                else
                {
                    _logWindow.AppendLog("No output file specified for MAMEFull.xml. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("No input file selected. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in CreateMAMEFull_Click");
        }
    }

    private async void CreateMAMEManufacturer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var inputFilePath = openFileDialog.FileName;

                _logWindow.AppendLog("Select Output Folder.");
                var folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var outputFolderMameManufacturer = folderBrowserDialog.SelectedPath;

                    try
                    {
                        var inputDoc = XDocument.Load(inputFilePath);

                        var progress = new Progress<int>(value =>
                        {
                            OverallProgress = value;
                        });

                        await MameManufacturer.CreateAndSaveMameManufacturerAsync(inputDoc, outputFolderMameManufacturer, progress, _logWindow);
                        _logWindow.AppendLog("Data extracted and saved successfully for all manufacturers.");
                    }
                    catch (Exception ex)
                    {
                        _logWindow.AppendLog("An error occurred: " + ex.Message);
                        await LogError.LogAsync(ex, $"Error processing manufacturer data from file: {inputFilePath}");
                    }
                }
                else
                {
                    _logWindow.AppendLog("No output folder specified. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("No input file selected. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in CreateMAMEManufacturer_Click");
        }
    }

    private async void CreateMAMEYear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var inputFilePath = openFileDialog.FileName;

                _logWindow.AppendLog("Select Output Folder.");
                var folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var outputFolderMameYear = folderBrowserDialog.SelectedPath;

                    try
                    {
                        var inputDoc = XDocument.Load(inputFilePath);

                        var progress = new Progress<int>(value =>
                        {
                            OverallProgress = value;
                        });

                        await Task.Run(() => MameYear.CreateAndSaveMameYear(inputDoc, outputFolderMameYear, progress, _logWindow));
                        _logWindow.AppendLog("XML files created successfully for all years.");
                    }
                    catch (Exception ex)
                    {
                        _logWindow.AppendLog("An error occurred: " + ex.Message);
                        await LogError.LogAsync(ex, $"Error processing year data from file: {inputFilePath}");
                    }
                }
                else
                {
                    _logWindow.AppendLog("No output folder specified. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("No input file selected. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in CreateMAMEYear_Click");
        }
    }

    private async void CreateMAMESourcefile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var inputFilePath = openFileDialog.FileName;

                _logWindow.AppendLog("Select Output Folder.");
                FolderBrowserDialog folderBrowserDialog = new()
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var outputFolderMameSourcefile = folderBrowserDialog.SelectedPath;

                    try
                    {
                        var inputDoc = XDocument.Load(inputFilePath);

                        var progress = new Progress<int>(value =>
                        {
                            OverallProgress = value;
                        });

                        await MameSourcefile.CreateAndSaveMameSourcefileAsync(inputDoc, outputFolderMameSourcefile, progress, _logWindow);
                        _logWindow.AppendLog("Data extracted and saved successfully for all source files.");
                    }
                    catch (Exception ex)
                    {
                        _logWindow.AppendLog("An error occurred: " + ex.Message);
                        await LogError.LogAsync(ex, $"Error processing sourcefile data from file: {inputFilePath}");
                    }
                }
                else
                {
                    _logWindow.AppendLog("No output folder specified. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("No input file selected. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in CreateMAMESourcefile_Click");
        }
    }

    private async void MergeLists_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select XML files to merge. You can select multiple XML files.");
            OpenFileDialog openFileDialog = new()
            {
                Title = "Select XML files to merge",
                Filter = "XML files (*.xml)|*.xml",
                Multiselect = true // Enable multiple file selection
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string[] inputFilePaths = openFileDialog.FileNames; // Get all selected file paths

                _logWindow.AppendLog("Select where to save the merged XML file.");
                SaveFileDialog saveFileDialog = new()
                {
                    Title = "Save Merged XML",
                    Filter = "XML files (*.xml)|*.xml",
                    FileName = "Merged.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var outputXmlPath = saveFileDialog.FileName;

                    // Create DAT filename based on XML filename (replace extension)
                    var outputDatPath = Path.ChangeExtension(outputXmlPath, ".dat");

                    try
                    {
                        // Use the new method that creates both XML and DAT files
                        MergeList.MergeAndSaveBoth(inputFilePaths, outputXmlPath, outputDatPath, _logWindow);
                        _logWindow.AppendLog($"Merging completed. Created XML file ({outputXmlPath}) and DAT file ({outputDatPath}).");

                        OverallProgress = 100;
                    }
                    catch (Exception ex)
                    {
                        _logWindow.AppendLog("An error occurred: " + ex.Message);
                        await LogError.LogAsync(ex, "Error merging XML files");
                    }
                }
                else
                {
                    _logWindow.AppendLog("No output file specified for merged XML. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("No input file selected. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in MergeLists_Click");
        }
    }

    private async void CopyRoms_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select the source directory containing the ROMs.");
            var sourceFolderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the source directory containing the ROMs"
            };

            if (sourceFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var sourceDirectory = sourceFolderBrowserDialog.SelectedPath;

                _logWindow.AppendLog("Select the destination directory for the ROMs.");
                var destinationFolderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select the destination directory for the ROMs"
                };

                if (destinationFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var destinationDirectory = destinationFolderBrowserDialog.SelectedPath;

                    _logWindow.AppendLog("Please select the XML file(s) containing ROM information. You can select multiple XML files.");
                    OpenFileDialog openFileDialog = new()
                    {
                        Title = "Please select the XML file(s) containing ROM information",
                        Filter = "XML Files (*.xml)|*.xml",
                        Multiselect = true
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        string[] xmlFilePaths = openFileDialog.FileNames;

                        try
                        {
                            var progress = new Progress<int>(value =>
                            {
                                OverallProgress = value;
                            });

                            await CopyRoms.CopyRomsFromXmlAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, _logWindow);
                            _logWindow.AppendLog("ROM copy operation is finished.");
                        }
                        catch (Exception ex)
                        {
                            _logWindow.AppendLog($"An error occurred: {ex.Message}");
                            await LogError.LogAsync(ex, "Error copying ROMs");
                        }
                    }
                    else
                    {
                        _logWindow.AppendLog("You did not provide the XML file(s) containing ROM information. Operation cancelled.");
                    }
                }
                else
                {
                    _logWindow.AppendLog("You did not select a destination directory for the ROMs. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("You did not provide the source directory containing the ROMs. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in CopyRoms_Click");
        }
    }

    private async void CopyImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select the source directory containing the images.");
            var sourceFolderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the source directory containing the images"
            };

            if (sourceFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var sourceDirectory = sourceFolderBrowserDialog.SelectedPath;

                _logWindow.AppendLog("Select the destination directory for the images.");
                var destinationFolderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select the destination directory for the images"
                };

                if (destinationFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var destinationDirectory = destinationFolderBrowserDialog.SelectedPath;

                    _logWindow.AppendLog("Please select the XML file(s) containing ROM information. You can select multiple XML files.");
                    OpenFileDialog openFileDialog = new()
                    {
                        Title = "Please select the XML file(s) containing ROM information",
                        Filter = "XML Files (*.xml)|*.xml",
                        Multiselect = true
                    };

                    if (openFileDialog.ShowDialog() != true) return;

                    string[] xmlFilePaths = openFileDialog.FileNames;

                    try
                    {
                        var progress = new Progress<int>(value =>
                        {
                            OverallProgress = value;
                        });

                        await CopyImages.CopyImagesFromXmlAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, _logWindow);
                        _logWindow.AppendLog("Image copy operation is finished.");
                    }
                    catch (Exception ex)
                    {
                        _logWindow.AppendLog("An error occurred: " + ex.Message);
                        await LogError.LogAsync(ex, "Error copying images");
                    }
                }
                else
                {
                    _logWindow.AppendLog("No destination directory selected. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("No source directory selected. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in CopyImages_Click");
        }
    }

    private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        OverallProgress = e.ProgressPercentage;
    }

    // Removed unused ProgressBarProgressReporter class

    private async void CreateMAMESoftwareList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OverallProgress = 0;

            _logWindow.AppendLog("Select the folder containing XML files to process.");
            using var folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select the folder containing XML files to process";

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var inputFolderPath = folderBrowserDialog.SelectedPath;

                _logWindow.AppendLog("Choose a location to save the consolidated output XML file.");
                SaveFileDialog saveFileDialog = new()
                {
                    Title = "Save Consolidated XML File",
                    Filter = "XML Files (*.xml)|*.xml",
                    FileName = "MAMESoftwareList.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var outputFilePath = saveFileDialog.FileName;

                    try
                    {
                        var progress = new Progress<int>(value =>
                        {
                            OverallProgress = value;
                        });

                        MameSoftwareList.CreateAndSaveSoftwareList(inputFolderPath, outputFilePath, progress, _logWindow);
                        _logWindow.AppendLog("Consolidated XML file created successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logWindow.AppendLog("An error occurred: " + ex.Message);
                        await LogError.LogAsync(ex, "Error creating software list");
                    }
                }
                else
                {
                    _logWindow.AppendLog("No output file specified. Operation cancelled.");
                }
            }
            else
            {
                _logWindow.AppendLog("No folder selected. Operation cancelled.");
            }
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in CreateMAMESoftwareList_Click");
        }
    }

    public void Dispose()
    {
        // Unregister event handlers to prevent memory leaks
        if (true)
        {
            _worker.ProgressChanged -= Worker_ProgressChanged;
            // BackgroundWorker doesn't implement IDisposable, but we should remove event handlers
        }

        // Suppress finalization since we've manually disposed resources
        GC.SuppressFinalize(this);
    }
}