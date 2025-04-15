using System.IO;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

public static class CopyImages
{
    public static async Task CopyImagesFromXmlAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, ILogService logService)
    {
        var totalFiles = xmlFilePaths.Length;
        var filesProcessed = 0;
        var overallProgressPercentage = 0; // Track overall progress

        // Define logging intervals
        const int logInterval = 5; // Log progress every 5 files

        logService.Log($"Starting image copy operation. Files to process: {totalFiles}");
        logService.Log($"Source directory: {sourceDirectory}");
        logService.Log($"Destination directory: {destinationDirectory}");

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
                    // Calculate weighted progress
                    var weightedPercentage = (double)processed / totalFiles * 100 + (double)percent / totalFiles;
                    overallProgressPercentage = (int)weightedPercentage;
                    progress.Report(overallProgressPercentage);
                });

                // ProcessXmlFileAsync now handles its own progress reporting via fileProgress
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
                // Optionally continue
            }
        }

        logService.Log($"Image copy operation completed. Processed {filesProcessed} of {totalFiles} files.");
    }

    private static async Task ProcessXmlFileAsync(string xmlFilePath, string sourceDirectory, string destinationDirectory, IProgress<int> progress, ILogService logService)
    {
        XDocument xmlDoc;
        var fileName = Path.GetFileName(xmlFilePath); // Cache filename

        try
        {
            // Load XML asynchronously
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

        var totalMachines = machineNames.Count;
        if (totalMachines == 0)
        {
            logService.Log($"No machine entries found in {fileName}.");
            progress.Report(100); // Report 100% for this empty file
            return;
        }

        var machinesProcessed = 0;

        // Define logging intervals for internal processing
        const int internalLogInterval = 100; // Log every 100 machines
        const int internalProgressInterval = 50; // Update progress every 50 machines

        logService.Log($"Found {totalMachines} machine entries in {fileName}. Starting sequential image copy check...");

        // *** MODIFICATION: Use sequential foreach loop ***
        foreach (var machineName in machineNames)
        {
            // ProcessMachine is now synchronous
            if (machineName != null) ProcessMachine(machineName, sourceDirectory, destinationDirectory, logService);

            machinesProcessed++;

            // Update progress at intervals
            if (machinesProcessed % internalProgressInterval == 0 || machinesProcessed == totalMachines)
            {
                var progressPercentage = (double)machinesProcessed / totalMachines * 100;
                progress.Report((int)progressPercentage);
            }

            // Log at intervals
            if (machinesProcessed % internalLogInterval == 0 || machinesProcessed == totalMachines)
            {
                logService.Log($"Image copy progress for {fileName}: {machinesProcessed}/{totalMachines} machines checked.");
            }
        }

        logService.Log($"Completed processing {machinesProcessed} machines from {fileName}");
        progress.Report(100); // Ensure 100% is reported for this file
    }

    // *** MODIFICATION: Made synchronous (removed async Task) ***
    private static void ProcessMachine(string machineName, string sourceDirectory, string destinationDirectory, ILogService logService)
    {
        try
        {
            // CopyImageFile is now synchronous
            CopyImageFile(sourceDirectory, destinationDirectory, machineName, "png", logService);
            CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpg", logService);
            CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpeg", logService);
        }
        catch (Exception ex) // Catch exceptions during the processing of a single machine's images
        {
            logService.LogError($"Error processing images for {machineName}: {ex.Message}");
            // Queue exception logging
            _ = logService.LogExceptionAsync(ex, $"Error processing images for {machineName}");
        }
    }

    // *** MODIFICATION: Made synchronous (removed Task.Run and async/Task return type) ***
    private static void CopyImageFile(string sourceDirectory, string destinationDirectory, string? machineName, string extension, ILogService logService)
    {
        if (string.IsNullOrEmpty(machineName))
        {
            logService.LogWarning($"Machine name is null or empty when checking for extension: {extension}");
            return;
        }

        var sourceFile = Path.Combine(sourceDirectory, machineName + "." + extension);
        var destinationFile = Path.Combine(destinationDirectory, machineName + "." + extension);

        try
        {
            if (File.Exists(sourceFile))
            {
                // logService.Log($"Copying: {machineName}.{extension} to {destinationDirectory}"); // Optional: Verbose
                File.Copy(sourceFile, destinationFile, true); // Overwrite if exists
                // logService.Log($"Copied: {machineName}.{extension}"); // Optional: Verbose
            }
            // No need to log if file not found unless debugging verbosely
            // else
            // {
            //     logService.Log($"Image file not found: {sourceFile}");
            // }
        }
        catch (IOException ioEx)
        {
            logService.LogError($"IO Error copying {machineName}.{extension}: {ioEx.Message}");
            _ = logService.LogExceptionAsync(ioEx, $"IO Error copying {machineName}.{extension}");
        }
        catch (Exception ex)
        {
            logService.LogError($"Failed to copy {machineName}.{extension}: {ex.Message}");
            _ = logService.LogExceptionAsync(ex, $"Failed to copy {machineName}.{extension}");
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