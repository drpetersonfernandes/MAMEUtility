using System.IO;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;
using System.Threading;

namespace MAMEUtility;

public static class CopyRoms
{
    public static async Task CopyRomsFromXmlAsync(
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

        logService.Log($"Starting ROM copy operation. Files to process: {totalFiles}");
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
                var fileProgress = new Progress<int>(percent =>
                {
                    // Calculate weighted progress
                    var weightedPercentage = (double)filesProcessed / totalFiles * 100 + (double)percent / totalFiles;
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

                filesProcessed++;

                // Batch logging
                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logService.Log($"Overall Progress: {filesProcessed}/{totalFiles} XML files processed.");
                }

                progress.Report((int)((double)filesProcessed / totalFiles * 100));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logService.LogError($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"An error occurred processing {Path.GetFileName(xmlFilePath)}");
            }
        }

        logService.Log($"ROM copy operation completed. Processed {filesProcessed} of {totalFiles} files.");
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

        var totalRoms = machineNames.Count;
        if (totalRoms == 0)
        {
            logService.Log($"No machine entries found in {fileName}.");
            progress.Report(100);
            return;
        }

        var romsProcessed = 0;
        const int internalLogInterval = 100;
        const int internalProgressInterval = 50;

        logService.Log($"Found {totalRoms} machine entries in {fileName}. Starting sequential copy...");

        foreach (var machineName in machineNames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Throughput optimization
                if (romsProcessed % 10 == 0)
                {
                    await Task.Delay(1, cancellationToken); // Yield to thread pool
                }

                await Task.Run(() => 
                    CopyRom(sourceDirectory, destinationDirectory, machineName, logService),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logService.LogError($"Error copying ROM for {machineName}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"Error copying ROM for {machineName}");
            }

            romsProcessed++;

            // Batch logging
            if (romsProcessed % internalLogInterval == 0 || romsProcessed == totalRoms)
            {
                logService.Log($"ROM copy progress for {fileName}: {romsProcessed}/{totalRoms}");
            }

            // Progress reporting
            if (romsProcessed % internalProgressInterval == 0 || romsProcessed == totalRoms)
            {
                var progressPercentage = (double)romsProcessed / totalRoms * 100;
                progress.Report((int)progressPercentage);
            }
        }

        logService.Log($"Completed processing {romsProcessed} ROMs from {fileName}");
        progress.Report(100);
    }

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

            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, destinationFile, true); // Overwrite existing files
            }
            else
            {
                logService.Log($"ROM Source file not found: {sourceFile}");
            }
        }
        catch (IOException ioEx)
        {
            logService.LogError($"IO Error copying ROM for {machineName}: {ioEx.Message}");
            _ = logService.LogExceptionAsync(ioEx, $"IO Error copying ROM for {machineName}");
        }
        catch (Exception ex)
        {
            logService.LogError($"An error occurred copying ROM for {machineName}: {ex.Message}");
            _ = logService.LogExceptionAsync(ex, $"An error occurred copying ROM for {machineName}");
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
