using System.IO;
using System.Xml.Linq;

namespace MAMEUtility;

public static class CopyRoms
{
    public static async Task CopyRomsFromXmlAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, LogWindow logWindow)
    {
        var totalFiles = xmlFilePaths.Length;
        var filesProcessed = 0;

        // Define logging intervals
        const int logInterval = 5; // Log progress every 5 files
        const int progressInterval = 2; // Update progress every 2 files

        logWindow.AppendLog($"Starting ROM copy operation. Files to process: {totalFiles}");
        logWindow.AppendLog($"Source directory: {sourceDirectory}");
        logWindow.AppendLog($"Destination directory: {destinationDirectory}");

        // Validate directories first
        if (!Directory.Exists(sourceDirectory))
        {
            logWindow.AppendLog($"Source directory does not exist: {sourceDirectory}");
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirectory}");
        }

        // Create destination directory if it doesn't exist
        if (!Directory.Exists(destinationDirectory))
        {
            try
            {
                Directory.CreateDirectory(destinationDirectory);
                logWindow.AppendLog($"Created destination directory: {destinationDirectory}");
            }
            catch (Exception ex)
            {
                await LogError.LogAsync(ex, $"Failed to create destination directory: {destinationDirectory}");
                throw;
            }
        }

        // Process each XML file - we'll keep this sequential to avoid overwhelming the file system
        foreach (var xmlFilePath in xmlFilePaths)
        {
            try
            {
                await ProcessXmlFileAsync(xmlFilePath, sourceDirectory, destinationDirectory, progress, logWindow);

                filesProcessed++;

                // Log progress at intervals
                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logWindow.AppendLog($"Progress: {filesProcessed}/{totalFiles} XML files processed.");
                }

                // Update progress bar at intervals
                if (filesProcessed % progressInterval != 0 && filesProcessed != totalFiles) continue;

                var progressPercentage = (double)filesProcessed / totalFiles * 100;
                progress.Report((int)progressPercentage);
            }
            catch (Exception ex)
            {
                logWindow.AppendLog($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                await LogError.LogAsync(ex, $"An error occurred processing {Path.GetFileName(xmlFilePath)}");
            }
        }

        logWindow.AppendLog($"ROM copy operation completed. Processed {filesProcessed} of {totalFiles} files.");
    }

    private static async Task ProcessXmlFileAsync(string xmlFilePath, string sourceDirectory, string destinationDirectory, IProgress<int> progress, LogWindow logWindow)
    {
        XDocument xmlDoc;

        try
        {
            xmlDoc = await Task.Run(() => XDocument.Load(xmlFilePath));
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, $"Failed to load XML file: {xmlFilePath}");
            throw;
        }

        // Validate the XML document structure
        if (!ValidateXmlStructure(xmlDoc))
        {
            var message = $"The file {Path.GetFileName(xmlFilePath)} does not match the required XML structure. Operation cancelled.";
            logWindow.AppendLog(message);
            return;
        }

        var machineNames = xmlDoc.Descendants("Machine")
            .Select(static machine => machine.Element("MachineName")?.Value)
            .Where(static name => !string.IsNullOrEmpty(name))
            .ToList();

        var totalRoms = machineNames.Count;
        var romsProcessed = 0;

        // Define logging intervals for internal processing
        const int internalLogInterval = 100; // Log every 100 ROMs
        const int internalProgressInterval = 50; // Update progress every 50 ROMs

        logWindow.AppendLog($"Found {totalRoms} machine entries in {Path.GetFileName(xmlFilePath)}");

        // Use parallel processing with throttling for ROM copying
        var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
        var sync = new object(); // For thread-safe progress updates

        await Parallel.ForEachAsync(machineNames, options, async (machineName, token) =>
        {
            await CopyRomAsync(sourceDirectory, destinationDirectory, machineName, logWindow);

            // Thread-safe incrementing
            lock (sync)
            {
                romsProcessed++;
                var processed = romsProcessed;

                // Update progress at intervals
                if (processed % internalProgressInterval == 0 || processed == totalRoms)
                {
                    var progressPercentage = (double)processed / totalRoms * 100;
                    progress.Report((int)progressPercentage);
                }

                // Log at intervals - inside lock to avoid log interleaving
                if (processed % internalLogInterval == 0 || processed == totalRoms)
                {
                    logWindow.AppendLog($"ROM copy progress: {processed}/{totalRoms} from {Path.GetFileName(xmlFilePath)}");
                }
            }
        });

        logWindow.AppendLog($"Completed processing {romsProcessed} ROMs from {Path.GetFileName(xmlFilePath)}");
    }

    private static Task CopyRomAsync(string sourceDirectory, string destinationDirectory, string? machineName, LogWindow logWindow)
    {
        return Task.Run(async () =>
        {
            logWindow.AppendLog($"Attempting to copy ROM for machine: {machineName}");
            try
            {
                var sourceFile = Path.Combine(sourceDirectory, machineName + ".zip");
                var destinationFile = Path.Combine(destinationDirectory, machineName + ".zip");

                logWindow.AppendLog($"Source file path: {sourceFile}");
                logWindow.AppendLog($"Destination file path: {destinationFile}");

                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destinationFile, true);
                    logWindow.AppendLog($"Successfully copied: {machineName}.zip to {destinationDirectory}");
                }
                else
                {
                    logWindow.AppendLog($"File not found: {sourceFile}");
                }
            }
            catch (Exception ex)
            {
                logWindow.AppendLog($"An error occurred copying ROM for {machineName}: {ex.Message}");
                await LogError.LogAsync(ex, $"An error occurred copying ROM for {machineName}");
            }
        });
    }

    private static bool ValidateXmlStructure(XDocument xmlDoc)
    {
        // Check if the root element is "Machines" and if it contains at least one "Machine" element
        // with both "MachineName" and "Description" child elements.
        var isValid = xmlDoc.Root?.Name.LocalName == "Machines" &&
                      xmlDoc.Descendants("Machine").Any(static machine =>
                          machine.Element("MachineName") != null &&
                          machine.Element("Description") != null);

        return isValid;
    }
}