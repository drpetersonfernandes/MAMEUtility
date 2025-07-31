using System.IO;
using System.Xml.Linq;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class CopyImages
{
    public static async Task CopyImagesFromXmlAsync(
        string[] xmlFilePaths,
        string sourceDirectory,
        string destinationDirectory,
        IProgress<int> progress,
        ILogService logService,
        CancellationToken cancellationToken = default)
    {
        var totalFiles = xmlFilePaths.Length;
        var filesProcessed = 0;
        const int logInterval = 5; // Log progress every 5 files

        logService.Log($"Starting image copy operation. Files to process: {totalFiles}");
        logService.Log($"Source directory: {sourceDirectory}");
        logService.Log($"Destination directory: {destinationDirectory}");

        // Validate directories
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
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var currentFileIndex = filesProcessed; // Capture current index before processing
                var fileProgress = new Progress<int>(percent =>
                {
                    // Calculate weighted progress: (completed files + current file's percentage) / total files * 100
                    var weightedPercentage = (currentFileIndex + percent / 100.0) / totalFiles * 100.0;
                    progress.Report((int)weightedPercentage);
                });

                await ProcessXmlFileAsync(
                    xmlFilePath,
                    sourceDirectory,
                    destinationDirectory,
                    fileProgress,
                    logService,
                    cancellationToken
                );

                filesProcessed++; // Increment only after the file is fully processed

                // Batch logging
                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logService.Log($"Overall Progress: {filesProcessed}/{totalFiles} XML files processed.");
                }

                // Report 100% for this file's contribution to overall progress
                progress.Report((int)((double)filesProcessed / totalFiles * 100));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logService.LogError($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"An error occurred processing {Path.GetFileName(xmlFilePath)}");
                // Even if an error occurs, we still count it as "processed" for overall progress calculation
                // to ensure the progress bar eventually reaches 100% for the total number of files attempted.
                filesProcessed++;
                progress.Report((int)((double)filesProcessed / totalFiles * 100));
            }
        }

        logService.Log($"Image copy operation completed. Processed {filesProcessed} of {totalFiles} files.");
    }

    private static async Task ProcessXmlFileAsync(
        string xmlFilePath,
        string sourceDirectory,
        string destinationDirectory,
        IProgress<int> progress,
        ILogService logService,
        CancellationToken cancellationToken)
    {
        XDocument xmlDoc;
        var fileName = Path.GetFileName(xmlFilePath);

        try
        {
            xmlDoc = await Task.Run(() => XDocument.Load(xmlFilePath), cancellationToken);
            logService.Log($"Successfully loaded XML file: {fileName}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await logService.LogExceptionAsync(ex, $"Failed to load XML file: {xmlFilePath}");
            logService.LogError($"Failed to load XML file: {fileName}. Skipping this file.");
            return;
        }

        if (!ValidateXmlStructure(xmlDoc))
        {
            logService.LogWarning($"The file {fileName} does not match the required XML structure. Skipping this file.");
            return;
        }

        var machineNames = xmlDoc.Descendants("Machine")
            .Select(static machine => machine.Element("MachineName")?.Value)
            .Where(static name => !string.IsNullOrEmpty(name))
            .ToList();

        var totalMachines = machineNames.Count;
        if (totalMachines == 0)
        {
            logService.Log($"No machine entries found in {fileName}.");
            progress.Report(100);
            return;
        }

        var machinesProcessed = 0;
        const int internalLogInterval = 100;
        const int internalProgressInterval = 50;

        logService.Log($"Found {totalMachines} machine entries in {fileName}. Starting sequential image copy...");

        foreach (var machineName in machineNames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await Task.Run(() =>
                        ProcessMachine(machineName, sourceDirectory, destinationDirectory, logService),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logService.LogError($"Error processing images for {machineName}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"Error processing images for {machineName}");
            }

            machinesProcessed++;

            // Batch logging
            if (machinesProcessed % internalLogInterval == 0 || machinesProcessed == totalMachines)
            {
                logService.Log($"Image copy progress for {fileName}: {machinesProcessed}/{totalMachines} machines processed");
            }

            // Progress reporting
            if (machinesProcessed % internalProgressInterval == 0 || machinesProcessed == totalMachines)
            {
                var progressPercentage = (double)machinesProcessed / totalMachines * 100;
                progress.Report((int)progressPercentage);
            }
        }

        logService.Log($"Completed processing {machinesProcessed} machines from {fileName}");
        progress.Report(100);
    }

    private static void ProcessMachine(string? machineName, string sourceDirectory, string destinationDirectory, ILogService logService)
    {
        try
        {
            CopyImageFile(sourceDirectory, destinationDirectory, machineName, "png", logService);
            CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpg", logService);
            CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpeg", logService);
        }
        catch (Exception ex)
        {
            logService.LogError($"Error processing images for {machineName}: {ex.Message}");
            _ = logService.LogExceptionAsync(ex, $"Error processing images for {machineName}");
        }
    }

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
                File.Copy(sourceFile, destinationFile, true); // Overwrite existing files
            }
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
        return xmlDoc.Root?.Name.LocalName == "Machines" &&
               xmlDoc.Descendants("Machine").Any(static machine =>
                   machine.Element("MachineName") != null &&
                   machine.Element("Description") != null);
    }
}
