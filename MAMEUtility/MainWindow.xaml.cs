using MAMEUtility;
using System.ComponentModel;
using System.Windows;
using System.Xml.Linq;

namespace MameUtility
{
    public partial class MainWindow
    {
        private readonly BackgroundWorker _worker;

        public MainWindow()
        {
            InitializeComponent();

            _worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            _worker.ProgressChanged += Worker_ProgressChanged;
        }

        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.buymeacoffee.com/purelogiccode",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Unable to open the link: " + ex.Message);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new();
            aboutWindow.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private async void CreateMAMEFull_Click(object sender, RoutedEventArgs e)
        {
            _worker.ReportProgress(0);

            Console.WriteLine("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                Console.WriteLine("Put a name to your output file.");
                Microsoft.Win32.SaveFileDialog saveFileDialog = new()
                {
                    Title = "Save MAMEFull",
                    Filter = "XML files (*.xml)|*.xml",
                    FileName = "MAMEFull.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string outputFilePathMameFull = saveFileDialog.FileName;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);
                        await MameFull.CreateAndSaveMameFullAsync(inputDoc, outputFilePathMameFull, _worker);
                        Console.WriteLine("Output file saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output file specified for MAMEFull.xml. Operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Operation cancelled.");
            }
        }

        private async void CreateMAMEManufacturer_Click(object sender, RoutedEventArgs e)
        {
            _worker.ReportProgress(0);

            Console.WriteLine("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                Console.WriteLine("Select Output Folder.");
                var folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolderMameManufacturer = folderBrowserDialog.SelectedPath;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);

                        var progress = new Progress<int>(value =>
                        {
                            ProgressBar.Value = value;
                        });

                        await MAMEManufacturer.CreateAndSaveMameManufacturerAsync(inputDoc, outputFolderMameManufacturer, progress);
                        Console.WriteLine("Data extracted and saved successfully for all manufacturers.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output folder specified. Operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Operation cancelled.");
            }
        }

        private async void CreateMAMEYear_Click(object sender, RoutedEventArgs e)
        {
            _worker.ReportProgress(0);

            Console.WriteLine("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                Console.WriteLine("Select Output Folder.");
                var folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolderMameYear = folderBrowserDialog.SelectedPath;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);

                        var progress = new Progress<int>(value =>
                        {
                            ProgressBar.Value = value;
                        });

                        await Task.Run(() => MameYear.CreateAndSaveMameYear(inputDoc, outputFolderMameYear, progress));
                        Console.WriteLine("XML files created successfully for all years.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output folder specified. Operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Operation cancelled.");
            }
        }

        private async void CreateMAMESourcefile_Click(object sender, RoutedEventArgs e)
        {
            _worker.ReportProgress(0);

            Console.WriteLine("Select MAME full driver information in XML. You can download this file from the MAME Website.");
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                Console.WriteLine("Select Output Folder.");
                FolderBrowserDialog folderBrowserDialog = new()
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolderMameSourcefile = folderBrowserDialog.SelectedPath;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);

                        var progress = new Progress<int>(value =>
                        {
                            ProgressBar.Value = value;
                        });

                        await MameSourcefile.CreateAndSaveMameSourcefileAsync(inputDoc, outputFolderMameSourcefile, progress);
                        Console.WriteLine("Data extracted and saved successfully for all source files.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output folder specified. Operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Operation cancelled.");
            }
        }

        private void MergeLists_Click(object sender, RoutedEventArgs e)
        {
            _worker.ReportProgress(0);

            Console.WriteLine("Select XML files to merge. You can select multiple XML files.");
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select XML files to merge",
                Filter = "XML files (*.xml)|*.xml",
                Multiselect = true // Enable multiple file selection
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string[] inputFilePaths = openFileDialog.FileNames; // Get all selected file paths

                Console.WriteLine("Put a name to your output file.");
                Microsoft.Win32.SaveFileDialog saveFileDialog = new()
                {
                    Title = "Save Merged XML",
                    Filter = "XML files (*.xml)|*.xml",
                    FileName = "Merged.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string outputFilePath = saveFileDialog.FileName;

                    try
                    {
                        MergeList.MergeAndSave(inputFilePaths, outputFilePath); // The method accept an array of file paths
                        Console.WriteLine("Merging is finished.");

                        _worker.ReportProgress(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output file specified for merged XML. Operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Operation cancelled.");
            }
        }

        private async void CopyRoms_Click(object sender, RoutedEventArgs e)
        {
            _worker.ReportProgress(0);

            Console.WriteLine("Select the source directory containing the ROMs.");
            var sourceFolderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the source directory containing the ROMs"
            };

            if (sourceFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string sourceDirectory = sourceFolderBrowserDialog.SelectedPath;

                Console.WriteLine("Select the destination directory for the ROMs.");
                var destinationFolderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select the destination directory for the ROMs"
                };

                if (destinationFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string destinationDirectory = destinationFolderBrowserDialog.SelectedPath;

                    Console.WriteLine("Please select the XML file(s) containing ROM information. You can select multiple XML files.");
                    Microsoft.Win32.OpenFileDialog openFileDialog = new()
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
                                ProgressBar.Value = value;
                            });

                            await CopyRoms.CopyRomsFromXmlAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress);
                            Console.WriteLine("ROM copy operation is finished.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred: {ex.Message}");
                        }

                        _worker.ReportProgress(100);
                    }
                    else
                    {
                        Console.WriteLine("You did not provide the XML file(s) containing ROM information. Operation cancelled.");
                    }
                }
                else
                {
                    Console.WriteLine("You did not select a destination directory for the ROMs. Operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("You did not provide the source directory containing the ROMs. Operation cancelled.");
            }
        }

        private async void CopyImages_Click(object sender, RoutedEventArgs e)
        {
            _worker.ReportProgress(0);

            Console.WriteLine("Select the source directory containing the images.");
            var sourceFolderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the source directory containing the images"
            };

            if (sourceFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string sourceDirectory = sourceFolderBrowserDialog.SelectedPath;

                Console.WriteLine("Select the destination directory for the images.");
                var destinationFolderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select the destination directory for the images"
                };

                if (destinationFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string destinationDirectory = destinationFolderBrowserDialog.SelectedPath;

                    Console.WriteLine("Please select the XML file(s) containing ROM information. You can select multiple XML files.");
                    Microsoft.Win32.OpenFileDialog openFileDialog = new()
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
                            var progressReporter = new ProgressBarProgressReporter(_worker);

                            await CopyImages.CopyImagesFromXmlAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progressReporter);
                            Console.WriteLine("Image copy operation is finished.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occurred: " + ex.Message);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No destination directory selected. Operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("No source directory selected. Operation cancelled.");
            }
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() => { ProgressBar.Value = e.ProgressPercentage; });
        }

        public class ProgressBarProgressReporter(BackgroundWorker worker) : IProgress<int>
        {
            private readonly BackgroundWorker _worker = worker;

            public void Report(int value)
            {
                _worker.ReportProgress(value);
            }
        }

        private void CreateMAMESoftwareList_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}