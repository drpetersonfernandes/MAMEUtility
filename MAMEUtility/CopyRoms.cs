using System.IO;
using System.Xml.Linq;

namespace MAMEUtility
{
    public static class CopyRoms
    {
        public static async Task CopyRomsFromXmlAsync(string[] xmlFilePaths, string sourceDirectory, IProgress<int> progress)
        {
            int totalFiles = xmlFilePaths.Length;
            int filesProcessed = 0;

            foreach (string xmlFilePath in xmlFilePaths)
            {
                try
                {
                    await ProcessXmlFileAsync(xmlFilePath, sourceDirectory, progress);
                    filesProcessed++;
                    double progressPercentage = (double)filesProcessed / totalFiles * 100;
                    progress.Report((int)progressPercentage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                }
            }
        }

        private static async Task ProcessXmlFileAsync(string xmlFilePath, string sourceDirectory, IProgress<int> progress)
        {
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            var machineNames = xmlDoc.Descendants("Machine")
                                     .Select(machine => machine.Element("MachineName")?.Value)
                                     .Where(name => !string.IsNullOrEmpty(name))
                                     .ToList();

            int totalRoms = machineNames.Count;
            int romsProcessed = 0;

            foreach (var machineName in machineNames)
            {
                if (machineName != null)
                {
                    await CopyRomAsync(sourceDirectory, machineName);

                    romsProcessed++;
                    double progressPercentage = (double)romsProcessed / totalRoms * 100;
                    progress.Report((int)progressPercentage);
                }
                else
                {
                    Console.WriteLine("Invalid machine name: null");
                }
            }
        }

        private static Task CopyRomAsync(string sourceDirectory, string machineName)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (machineName != null)
                    {
                        string sourceFile = Path.Combine(sourceDirectory, machineName + ".zip");

                        if (File.Exists(sourceFile))
                        {
                            string destinationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Default destination directory
                            string destinationFile = Path.Combine(destinationDirectory, machineName + ".zip");

                            File.Copy(sourceFile, destinationFile, overwrite: true);
                            Console.WriteLine($"Copied: {machineName}.zip to {destinationDirectory}");
                        }
                        else
                        {
                            Console.WriteLine($"File not found: {machineName}.zip");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Invalid machine name: {machineName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred copying ROM for {machineName}: {ex.Message}");
                }
            });
        }

    }
}