using System.IO;
using System.Xml;
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
        var filesProcessedCount = 0;
        const int logInterval = 5;

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

        logService.BeginBatchOperation();
        try
        {
            foreach (var xmlFilePath in xmlFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var currentFileIndex = filesProcessedCount;
                    var fileProgress = new Progress<int>(percent =>
                    {
                        var clampedPercent = Math.Max(0, Math.Min(100, percent));
                        var weightedPercentage = (currentFileIndex + clampedPercent / 100.0) / totalFiles * 100.0;
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

                    filesProcessedCount++;

                    if (filesProcessedCount % logInterval == 0 || filesProcessedCount == totalFiles)
                    {
                        logService.Log($"Overall Progress: {filesProcessedCount}/{totalFiles} XML files processed.");
                    }

                    progress.Report((int)((double)filesProcessedCount / totalFiles * 100));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logService.LogError($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
                    await logService.LogExceptionAsync(ex, $"An error occurred processing {Path.GetFileName(xmlFilePath)}");
                    filesProcessedCount++;
                    var progressPercentage = Math.Min(100, (int)((double)filesProcessedCount / totalFiles * 100));
                    progress.Report(progressPercentage);
                }
            }
        }
        finally
        {
            logService.EndBatchOperation("Image Copy Completed");
        }

        logService.Log($"Image copy operation completed. Processed {filesProcessedCount} of {totalFiles} files.");
    }

    private static async Task ProcessXmlFileAsync(
        string xmlFilePath,
        string sourceDirectory,
        string destinationDirectory,
        IProgress<int> progress,
        ILogService logService,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(xmlFilePath);

        try
        {
            logService.Log($"Processing XML file: {fileName}");

            var machineNames = new List<string>();
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreWhitespace = true,
                Async = true
            };

            await using var fileStream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read);
            using (var reader = XmlReader.Create(fileStream, settings))
            {
                while (await reader.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element && string.Equals(reader.Name, "Machine", StringComparison.OrdinalIgnoreCase))
                    {
                        using var subReader = reader.ReadSubtree();
                        while (await subReader.ReadAsync())
                        {
                            if (subReader.NodeType == XmlNodeType.Element && string.Equals(subReader.Name, "MachineName", StringComparison.OrdinalIgnoreCase))
                            {
                                var name = await subReader.ReadElementContentAsStringAsync();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    machineNames.Add(name);
                                }

                                break;
                            }
                        }
                    }
                }
            }

            var totalMachines = machineNames.Count;
            if (totalMachines == 0)
            {
                logService.Log($"No machine entries found in {fileName}.");
                progress.Report(100);
                return;
            }

            var machinesProcessedCount = 0;
            var missingImagesCount = 0;
            const int maxWarnings = 50;
            const int internalLogInterval = 100;
            const int internalProgressInterval = 50;

            logService.Log($"Found {totalMachines} machine entries in {fileName}. Starting sequential image copy...");

            // Offload the synchronous file copy loop to a background thread to prevent UI freezing
            await Task.Run(() =>
            {
                foreach (var machineName in machineNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        ProcessMachine(machineName, sourceDirectory, destinationDirectory, logService, ref missingImagesCount, maxWarnings);
                    }
                    catch (Exception ex)
                    {
                        logService.LogError($"Error processing images for {machineName}: {ex.Message}");
                        logService.LogExceptionAsync(ex, $"Error processing images for {machineName}").GetAwaiter().GetResult();
                    }

                    machinesProcessedCount++;

                    if (machinesProcessedCount % internalLogInterval == 0 || machinesProcessedCount == totalMachines)
                    {
                        logService.Log($"Image copy progress for {fileName}: {machinesProcessedCount}/{totalMachines} machines processed");
                    }

                    if (machinesProcessedCount % internalProgressInterval == 0 || machinesProcessedCount == totalMachines)
                    {
                        var progressPercentage = (double)machinesProcessedCount / totalMachines * 100;
                        progress.Report((int)progressPercentage);
                    }
                }

                if (missingImagesCount > maxWarnings)
                {
                    logService.LogWarning($"{missingImagesCount} total image files were not found in {fileName}. Only the first {maxWarnings} were logged individually.");
                }
            }, cancellationToken);

            logService.Log($"Completed processing {machinesProcessedCount} machines from {fileName}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await logService.LogExceptionAsync(ex, $"Failed to process XML file: {xmlFilePath}");
            logService.LogError($"Failed to process XML file: {fileName}. Skipping this file.");
        }

        progress.Report(100);
    }

    private static void ProcessMachine(string? machineName, string sourceDirectory, string destinationDirectory, ILogService logService, ref int missingFilesCount, int maxWarnings)
    {
        CopyImageFile(sourceDirectory, destinationDirectory, machineName, "png", logService, ref missingFilesCount, maxWarnings);
        CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpg", logService, ref missingFilesCount, maxWarnings);
        CopyImageFile(sourceDirectory, destinationDirectory, machineName, "jpeg", logService, ref missingFilesCount, maxWarnings);
    }

    private static void CopyImageFile(string sourceDirectory, string destinationDirectory, string? machineName, string extension, ILogService logService, ref int missingFilesCount, int maxWarnings)
    {
        if (string.IsNullOrEmpty(machineName)) return;

        var sourceFile = Path.Combine(sourceDirectory, machineName + "." + extension);
        var destinationFile = Path.Combine(destinationDirectory, machineName + "." + extension);

        if (File.Exists(sourceFile))
        {
            File.Copy(sourceFile, destinationFile, true);
        }
        else
        {
            missingFilesCount++;
            if (missingFilesCount <= maxWarnings)
            {
                logService.LogWarning($"Source image file not found: {sourceFile}");
            }
        }
    }
}
