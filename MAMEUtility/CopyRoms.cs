using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

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
        const int logInterval = 5;

        logService.Log($"Starting ROM copy operation. Files to process: {totalFiles}");
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
                    var currentFileIndex = filesProcessed;
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

                    filesProcessed++;

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
                    filesProcessed++;
                    var progressPercentage = Math.Min(100, (int)((double)filesProcessed / totalFiles * 100));
                    progress.Report(progressPercentage);
                }
            }
        }
        finally
        {
            logService.EndBatchOperation("ROM Copy Completed");
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

                    if (reader is { NodeType: XmlNodeType.Element, Name: "Machine" })
                    {
                        using var subReader = reader.ReadSubtree();
                        while (await subReader.ReadAsync())
                        {
                            if (subReader is { NodeType: XmlNodeType.Element, Name: "MachineName" })
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
                    CopyRom(sourceDirectory, destinationDirectory, machineName, logService);
                }
                catch (Exception ex)
                {
                    logService.LogError($"Error copying ROM for {machineName}: {ex.Message}");
                    await logService.LogExceptionAsync(ex, $"Error copying ROM for {machineName}");
                }

                romsProcessed++;

                if (romsProcessed % internalLogInterval == 0 || romsProcessed == totalRoms)
                {
                    logService.Log($"ROM copy progress for {fileName}: {romsProcessed}/{totalRoms}");
                }

                if (romsProcessed % internalProgressInterval == 0 || romsProcessed == totalRoms)
                {
                    var progressPercentage = (double)romsProcessed / totalRoms * 100;
                    progress.Report((int)progressPercentage);
                }
            }

            logService.Log($"Completed processing {romsProcessed} ROMs from {fileName}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await logService.LogExceptionAsync(ex, $"Failed to process XML file: {xmlFilePath}");
            logService.LogError($"Failed to process XML file: {fileName}. Skipping this file.");
        }

        progress.Report(100);
    }

    private static void CopyRom(string sourceDirectory, string destinationDirectory, string? machineName, ILogService logService)
    {
        if (string.IsNullOrEmpty(machineName)) return;

        var sourceFile = Path.Combine(sourceDirectory, machineName + ".zip");
        var destinationFile = Path.Combine(destinationDirectory, machineName + ".zip");

        if (File.Exists(sourceFile))
        {
            File.Copy(sourceFile, destinationFile, true);
        }
        else
        {
            logService.Log($"Source ROM file not found: {sourceFile}");
        }
    }
}
