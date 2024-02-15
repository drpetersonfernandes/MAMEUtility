using System.IO;
using System.Xml.Linq;

namespace MAMEUtility
{
    public static class CopyRoms
    {
        public static void CopyRomsFromXml(string[] xmlFilePaths, string sourceDirectory)
        {
            foreach (string xmlFilePath in xmlFilePaths)
            {
                try
                {
                    // Load the XML document
                    XDocument xmlDoc = XDocument.Load(xmlFilePath);

                    // Get all machine names from the XML document
                    var machineNames = xmlDoc.Descendants("Machine")
                                            .Select(machine => machine.Element("MachineName")?.Value)
                                            .Where(name => !string.IsNullOrEmpty(name))
                                            .ToList();

                    // Prompt user for the destination directory
                    var destinationFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = "Select the destination directory for the ROMs"
                    };

                    if (destinationFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string destinationDirectory = destinationFolderBrowserDialog.SelectedPath;

                        // Ensure destination directory exists
                        Directory.CreateDirectory(destinationDirectory);

                        // Copy each corresponding .zip file to the destination directory
                        foreach (var machineName in machineNames)
                        {
                            string sourceFile = Path.Combine(sourceDirectory, machineName + ".zip");
                            string destinationFile = Path.Combine(destinationDirectory, machineName + ".zip");

                            // Check if the file exists before attempting to copy
                            if (File.Exists(sourceFile))
                            {
                                File.Copy(sourceFile, destinationFile, overwrite: true);
                                Console.WriteLine($"Copied: {machineName}.zip to {destinationDirectory}");
                            }
                            else
                            {
                                Console.WriteLine($"File not found: {machineName}.zip");
                            }
                        }
                        Console.WriteLine($"File copy operation is finished.");
                    }
                    else
                    {
                        Console.WriteLine("No destination directory selected, operation cancelled for this XML file.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}
