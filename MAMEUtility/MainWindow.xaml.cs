using MAMEUtility;
using System.Runtime.InteropServices;
using System.Windows;
using System.Xml.Linq;
using MessageBox = System.Windows.MessageBox;

namespace MameUtility
{
    public partial class MainWindow : Window
    {
        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AttachConsole(int dwProcessId);

        public MainWindow()
        {
            AttachConsole(-1); // Attach to parent console
            InitializeComponent();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("MAME Utility\nPure Logic Code\nVersion 1.0.0.1", "About");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void CreateMAMEFull_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                Microsoft.Win32.SaveFileDialog saveFileDialog = new()
                {
                    Title = "Save MAMEFull",
                    Filter = "XML files (*.xml)|*.xml",
                    FileName = "MAMEFull.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string outputFilePathMAMEFull = saveFileDialog.FileName;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);
                        MAMEFull.CreateAndSaveMAMEFull(inputDoc, outputFilePathMAMEFull);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output file specified for MAMEFull.xml. Exiting.");
                }

            }
            else
            {
                Console.WriteLine("No input file selected. Exiting.");
            }
        }

        private void CreateMAMEManufacturer_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                FolderBrowserDialog folderBrowserDialog = new()
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolderMAMEManufacturer = folderBrowserDialog.SelectedPath;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);
                        MAMEManufacturer.CreateAndSaveMAMEManufacturer(inputDoc, outputFolderMAMEManufacturer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output folder specified. Exiting.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Exiting.");
            }
        }

        private void CreateMAMEYear_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                FolderBrowserDialog folderBrowserDialog = new()
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolderMAMEYear = folderBrowserDialog.SelectedPath;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);
                        MAMEYear.CreateAndSaveMAMEYear(inputDoc, outputFolderMAMEYear);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output folder specified. Exiting.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Exiting.");
            }
        }

        private void CreateMAMESourcefile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Title = "Select MAME full driver information in XML",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFilePath = openFileDialog.FileName;

                FolderBrowserDialog folderBrowserDialog = new()
                {
                    Description = "Select Output Folder"
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolderMAMESourcefile = folderBrowserDialog.SelectedPath;

                    try
                    {
                        XDocument inputDoc = XDocument.Load(inputFilePath);
                        MAMESourcefile.CreateAndSaveMAMESourcefile(inputDoc, outputFolderMAMESourcefile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output folder specified. Exiting.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Exiting.");
            }
        }

        private void MergeLists_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new()
            {
                Title = "Select first XML file to merge",
                Filter = "XML files (*.xml)|*.xml"
            };

            Microsoft.Win32.OpenFileDialog openFileDialog2 = new()
            {
                Title = "Select second XML file to merge",
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog1.ShowDialog() == true && openFileDialog2.ShowDialog() == true)
            {
                string inputFilePath1 = openFileDialog1.FileName;
                string inputFilePath2 = openFileDialog2.FileName;

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
                        MergeList.MergeAndSave(inputFilePath1, inputFilePath2, outputFilePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No output file specified for merged XML. Exiting.");
                }
            }
            else
            {
                Console.WriteLine("No input file selected. Exiting.");
            }
        }

        private void CopyRoms_Click(object sender, RoutedEventArgs e)
        {
            // Prompt user to select the source directory
            Console.WriteLine("Select the source directory containing the ROMs.");
            var sourceFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the source directory containing the ROMs"
            };

            if (sourceFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string sourceDirectory = sourceFolderBrowserDialog.SelectedPath;

                // Initialize the OpenFileDialog
                Console.WriteLine("Please select the XML file(s) containing ROM information.");
                Microsoft.Win32.OpenFileDialog openFileDialog = new()
                {
                    Title = "Please select the XML file(s) containing ROM information",
                    Filter = "XML Files (*.xml)|*.xml",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == null)
                {
                    Console.WriteLine("You did not provide the XML file(s) containing ROM information.");
                    return;
                }
                else
                {
                    string[] xmlFilePaths = openFileDialog.FileNames;
                    CopyRoms.CopyRomsFromXml(xmlFilePaths, sourceDirectory);
                }
            }
        }


        private void CopyImages_Click(object sender, RoutedEventArgs e)
        {
            // Prompt user to select the source directory for the images
            Console.WriteLine("Select the source directory containing the images.");
            var sourceFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the source directory containing the images"
            };

            if (sourceFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string sourceDirectory = sourceFolderBrowserDialog.SelectedPath;

                // Prompt user to select the destination directory for the images
                Console.WriteLine("Select the destination directory for the images.");
                var destinationFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select the destination directory for the images"
                };

                if (destinationFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string destinationDirectory = destinationFolderBrowserDialog.SelectedPath;

                    // Initialize OpenFileDialog for XML file selection
                    Console.WriteLine("Please select the XML file(s) containing ROM information.");
                    Microsoft.Win32.OpenFileDialog openFileDialog = new()
                    {
                        Title = "Please select the XML file(s) containing ROM information",
                        Filter = "XML Files (*.xml)|*.xml",
                        Multiselect = true
                    };

#pragma warning disable CS8629 // Nullable value type may be null.
                    if ((bool)openFileDialog.ShowDialog())
                    {
                        string[] xmlFilePaths = openFileDialog.FileNames;

                        // Call the method from CopyImages class to copy images
                        CopyImages.CopyImagesFromXml(xmlFilePaths, sourceDirectory, destinationDirectory);
                        Console.WriteLine("Image copy operation is finished.");
                    }
#pragma warning restore CS8629 // Nullable value type may be null.
                }
                else
                {
                    Console.WriteLine("No destination directory selected, operation cancelled.");
                }
            }
            else
            {
                Console.WriteLine("No source directory selected, operation cancelled.");
            }
        }




    }
}