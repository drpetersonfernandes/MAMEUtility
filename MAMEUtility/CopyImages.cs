using System.ComponentModel;
using System.IO;
using System.Xml.Linq;

namespace MAMEUtility
{
    public static class CopyImages
    {
        public static event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public static void CopyImagesFromXml(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory)
        {
            int totalFiles = xmlFilePaths.Length;
            int filesCopied = 0;

            foreach (string xmlFilePath in xmlFilePaths)
            {
                try
                {
                    ProcessXmlFile(xmlFilePath, sourceDirectory, destinationDirectory);
                    filesCopied++;
                    ReportProgress(filesCopied, totalFiles);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                }
            }
        }

        private static void ProcessXmlFile(string xmlFilePath, string sourceDirectory, string destinationDirectory)
        {
            // Load the XML document
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            // Get all machine names from the XML document
            var machineNames = xmlDoc.Descendants("Machine")
                                     .Select(machine => machine.Element("MachineName")?.Value)
                                     .Where(name => !string.IsNullOrEmpty(name))
                                     .ToList();

            int totalImages = machineNames.Count;
            int imagesCopied = 0;

            // Copy each corresponding image file to the destination directory
            foreach (var machineName in machineNames)
            {
                CopyImageFile(sourceDirectory, destinationDirectory, machineName, "png");
                CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpg");
                CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpeg");

                imagesCopied++;
                ReportProgress(imagesCopied, totalImages);
            }
        }

        private static void CopyImageFile(string sourceDirectory, string destinationDirectory, string machineName, string extension)
        {
            string sourceFile = Path.Combine(sourceDirectory, machineName + "." + extension);
            string destinationFile = Path.Combine(destinationDirectory, machineName + "." + extension);

            // Check if the file exists before attempting to copy
            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, destinationFile, overwrite: true);
                Console.WriteLine($"Copied: {machineName}.{extension} to {destinationDirectory}");
            }
            else
            {
                Console.WriteLine($"File not found: {machineName}.{extension}");
            }
        }

        private static void ReportProgress(int filesCopied, int totalFiles)
        {
            double progressPercentage = (double)filesCopied / totalFiles * 100;
            ProgressChanged?.Invoke(null, new ProgressChangedEventArgs((int)progressPercentage, null));
        }
    }
}
