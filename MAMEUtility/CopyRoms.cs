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
        var overallProgressPercentage = 0; // Track overall progress across files

        // Define logging intervals
        const int logInterval = 5; // Log progress every 5 files

        logService.Log($"Starting ROM copy operation. Files to process: {totalFiles}");
        logService.Log($"Source directory: {sourceDirectory}");
        logService.Log($"Destination directory: {destinationDirectory}");

        // Validate directories first
        if (!Directory.Exists(sourceDirectory))
        {
            logService.LogError($"Source directory does not exist: {sourceDirectory}");
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirectory}");
        }

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

        // Process each XML file sequentially
        foreach (var xmlFilePath in xmlFilePaths)
        {
            try
            {
                // Pass a specific progress reporter for this file
                var processed = filesProcessed;
                var fileProgress = new Progress<int>(percent =>
                {
                    // Calculate weighted progress: progress within the current file relative to the total number of files
                    var weightedPercentage = (double)processed / totalFiles * 100 + (double)percent / totalFiles;
                    overallProgressPercentage = (int)weightedPercentage;
                    progress.Report(overallProgressPercentage);
                });

                await ProcessXmlFileAsync(xmlFilePath, sourceDirectory, destinationDirectory, fileProgress, logService);

                filesProcessed++;

                // Log overall progress
                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logService.Log($"Overall Progress: {filesProcessed}/{totalFiles} XML files processed.");
                }

                // Ensure final progress is 100% if all files processed
                if (filesProcessed == totalFiles)
                {
                    progress.Report(100);
                }
                else // Report progress based on completed files if not the last one
                {
                    progress.Report((int)((double)filesProcessed / totalFiles * 100));
                }
            }
            catch (Exception ex)
            {
                logService.LogError($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"An error occurred processing {Path.GetFileName(xmlFilePath)}");
                // Optionally decide whether to continue with the next file or stop
            }
        }

        logService.Log($"ROM copy operation completed. Processed {filesProcessed} of {totalFiles} files.");
    }

    private static async Task ProcessXmlFileAsync(string xmlFilePath, string sourceDirectory, string destinationDirectory, IProgress<int> progress, ILogService logService)
    {
        XDocument xmlDoc;
        var fileName = Path.GetFileName(xmlFilePath); // Cache filename

        try
        {
            // Load XML asynchronously off the UI thread if necessary
            xmlDoc = await Task.Run(() => XDocument.Load(xmlFilePath));
            logService.Log($"Successfully loaded XML file: {fileName}");
        }
        catch (Exception ex)
        {
            await logService.LogExceptionAsync(ex, $"Failed to load XML file: {xmlFilePath}");
            logService.LogError($"Failed to load XML file: {fileName}. Skipping this file.");
            return; // Skip this file
        }

        if (!ValidateXmlStructure(xmlDoc))
        {
            logService.LogWarning($"The file {fileName} does not match the required XML structure. Skipping this file.");
            return; // Skip this file
        }

        var machineNames = xmlDoc.Descendants("Machine")
            .Select(static machine => machine.Element("MachineName")?.Value)
            .Where(static name => !string.IsNullOrEmpty(name))
            .ToList();

        var totalRoms = machineNames.Count;
        if (totalRoms == 0)
        {
            logService.Log($"No machine entries found in {fileName}.");
            progress.Report(100); // Report 100% for this empty file
            return;
        }

        var romsProcessed = 0;

        // Define logging intervals for internal processing
        const int internalLogInterval = 100; // Log every 100 ROMs
        const int internalProgressInterval = 50; // Update progress every 50 ROMs

        logService.Log($"Found {totalRoms} machine entries in {fileName}. Starting sequential copy...");

        // *** MODIFICATION: Use sequential foreach loop ***
        foreach (var machineName in machineNames)
        {
            // No need for await here as CopyRom is now synchronous
            CopyRom(sourceDirectory, destinationDirectory, machineName, logService);

            romsProcessed++;

            // Update progress at intervals
            if (romsProcessed % internalProgressInterval == 0 || romsProcessed == totalRoms)
            {
                var progressPercentage = (double)romsProcessed / totalRoms * 100;
                progress.Report((int)progressPercentage);
            }

            // Log at intervals
            if (romsProcessed % internalLogInterval == 0 || romsProcessed == totalRoms)
            {
                logService.Log($"ROM copy progress for {fileName}: {romsProcessed}/{totalRoms}");
            }
        }

        logService.Log($"Completed processing {romsProcessed} ROMs from {fileName}");
        progress.Report(100); // Ensure 100% is reported for this file upon completion
    }

    // *** MODIFICATION: Made synchronous (removed Task.Run and async/Task return type) ***
    private static void CopyRom(string sourceDirectory, string destinationDirectory, string? machineName, ILogService logService)
    {
        if (string.IsNullOrEmpty(machineName))
        {
            logService.LogWarning("Attempted to copy ROM with null or empty machine name.");
            return;
        }

        try
        {
            var sourceFile = Path.Combine(sourceDirectory, machineName + ".zip");
            var destinationFile = Path.Combine(destinationDirectory, machineName + ".zip");

            // logService.Log($"Checking source: {sourceFile}"); // Optional: Verbose logging

            if (File.Exists(sourceFile))
            {
                // logService.Log($"Copying: {machineName}.zip to {destinationDirectory}"); // Optional: Verbose logging
                File.Copy(sourceFile, destinationFile, true); // Overwrite if exists
                // logService.Log($"Successfully copied: {machineName}.zip"); // Optional: Verbose logging
            }
            else
            {
                logService.Log($"ROM Source file not found: {sourceFile}");
            }
        }
        catch (IOException ioEx) // Catch specific IO errors
        {
            logService.LogError($"IO Error copying ROM for {machineName}: {ioEx.Message}");
            // LogExceptionAsync is async, but we are in a sync method.
            // Queue it to run without blocking the copy loop.
            _ = logService.LogExceptionAsync(ioEx, $"IO Error copying ROM for {machineName}");
        }
        catch (Exception ex) // Catch other potential errors
        {
            logService.LogError($"An error occurred copying ROM for {machineName}: {ex.Message}");
            _ = logService.LogExceptionAsync(ex, $"An error occurred copying ROM for {machineName}");
        }
    }

    private static bool ValidateXmlStructure(XDocument xmlDoc)
    {
        var isValid = xmlDoc.Root?.Name.LocalName == "Machines" &&
                      xmlDoc.Descendants("Machine").Any(static machine =>
                          machine.Element("MachineName") != null &&
                          machine.Element("Description") != null);
        return isValid;
    }
}