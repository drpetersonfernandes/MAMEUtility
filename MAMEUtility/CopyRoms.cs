using System.IO;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

public static class CopyRoms
{
    public static async Task CopyRomsFromXmlAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, ILogService logService)
    {
        var totalFiles = xmlFilePaths.Length;
        var filesProcessed = 0;

        // Define logging intervals
        const int logInterval = 5; // Log progress every 5 files
        const int progressInterval = 2; // Update progress every 2 files

        logService.Log($"Starting ROM copy operation. Files to process: {totalFiles}");
        logService.Log($"Source directory: {sourceDirectory}");
        logService.Log($"Destination directory: {destinationDirectory}");

        // Validate directories first
        if (!Directory.Exists(sourceDirectory))
        {
            logService.LogError($"Source directory does not exist: {sourceDirectory}");
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirectory}");
        }

        // Create destination directory if it doesn't exist
        if (!Directory.Exists(destinationDirectory))
        {
            try
            {
                Directory.CreateDirectory(destinationDirectory);
                logService.Log($"Created destination directory: {destinationDirectory}");
            }
            catch (Exception ex)
            {
                await logService.LogExceptionAsync(ex, $"Failed to create destination directory: {destinationDirectory}");
                throw;
            }
        }

        // Process each XML file - we'll keep this sequential to avoid overwhelming the file system
        foreach (var xmlFilePath in xmlFilePaths)
        {
            try
            {
                // Pass logService down
                await ProcessXmlFileAsync(xmlFilePath, sourceDirectory, destinationDirectory, progress, logService);

                filesProcessed++;

                // Log progress at intervals
                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logService.Log($"Progress: {filesProcessed}/{totalFiles} XML files processed.");
                }

                // Update progress bar at intervals
                if (filesProcessed % progressInterval != 0 && filesProcessed != totalFiles) continue;

                var progressPercentage = (double)filesProcessed / totalFiles * 100;
                progress.Report((int)progressPercentage);
            }
            catch (Exception ex)
            {
                logService.LogError($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"An error occurred processing {Path.GetFileName(xmlFilePath)}");
            }
        }

        logService.Log($"ROM copy operation completed. Processed {filesProcessed} of {totalFiles} files.");
    }

    private static async Task ProcessXmlFileAsync(string xmlFilePath, string sourceDirectory, string destinationDirectory, IProgress<int> progress, ILogService logService)
    {
        XDocument xmlDoc;

        try
        {
            xmlDoc = await Task.Run(() => XDocument.Load(xmlFilePath));
        }
        catch (Exception ex)
        {
            await logService.LogExceptionAsync(ex, $"Failed to load XML file: {xmlFilePath}");
            throw;
        }

        // Validate the XML document structure
        if (!ValidateXmlStructure(xmlDoc))
        {
            var message = $"The file {Path.GetFileName(xmlFilePath)} does not match the required XML structure. Operation cancelled.";
            logService.LogWarning(message);

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

        logService.Log($"Found {totalRoms} machine entries in {Path.GetFileName(xmlFilePath)}");

        // Use parallel processing with throttling for ROM copying
        var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
        var sync = new object(); // For thread-safe progress updates

        await Parallel.ForEachAsync(machineNames, options, async (machineName, token) =>
        {
            // Pass logService down
            await CopyRomAsync(sourceDirectory, destinationDirectory, machineName, logService);

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
                    logService.Log($"ROM copy progress: {processed}/{totalRoms} from {Path.GetFileName(xmlFilePath)}");
                }
            }
        });

        logService.Log($"Completed processing {romsProcessed} ROMs from {Path.GetFileName(xmlFilePath)}");
    }

    // Changed LogWindow logWindow to ILogService logService
    private static Task CopyRomAsync(string sourceDirectory, string destinationDirectory, string? machineName, ILogService logService)
    {
        return Task.Run(async () =>
        {
            // Replaced logWindow.AppendLog with logService.Log
            logService.Log($"Attempting to copy ROM for machine: {machineName}");
            try
            {
                var sourceFile = Path.Combine(sourceDirectory, machineName + ".zip");
                var destinationFile = Path.Combine(destinationDirectory, machineName + ".zip");

                logService.Log($"Source file path: {sourceFile}");
                logService.Log($"Destination file path: {destinationFile}");

                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destinationFile, true);
                    logService.Log($"Successfully copied: {machineName}.zip to {destinationDirectory}");
                }
                else
                {
                    logService.Log($"File not found: {sourceFile}");
                }
            }
            catch (Exception ex)
            {
                logService.LogError($"An error occurred copying ROM for {machineName}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"An error occurred copying ROM for {machineName}");
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