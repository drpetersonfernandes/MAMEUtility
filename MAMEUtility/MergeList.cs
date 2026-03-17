using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;
using MAMEUtility.Models;
using MessagePack;

namespace MAMEUtility;

public static class MergeList
{
    public static async Task MergeAndSaveBothAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        if (inputFilePaths.Length == 0)
        {
            logService.LogWarning("No input files provided. Operation cancelled.");
            progress.Report(100);
            return;
        }

        logService.Log($"Starting merge operation with {inputFilePaths.Length} files.");
        progress.Report(0);

        var uniqueMachines = new Dictionary<string, MachineInfo>(StringComparer.OrdinalIgnoreCase);
        var filesProcessedCount = 0;
        var totalFiles = inputFilePaths.Length;

        const double xmlProcessingWeight = 80.0;
        const double savingWeight = 20.0;

        var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, Async = true };

        foreach (var inputFilePath in inputFilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using var reader = XmlReader.Create(fileStream, readerSettings);

                while (await reader.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType != XmlNodeType.Element) continue;

                    var nodeName = reader.Name;
                    if (string.Equals(nodeName, "Machine", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(nodeName, "Software", StringComparison.OrdinalIgnoreCase))
                    {
                        string? machineName = null;
                        string? description = null;

                        using (var subReader = reader.ReadSubtree())
                        {
                            while (await subReader.ReadAsync())
                            {
                                if (subReader.NodeType == XmlNodeType.Element)
                                {
                                    var subNodeName = subReader.Name;
                                    if (string.Equals(subNodeName, "MachineName", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(subNodeName, "SoftwareName", StringComparison.OrdinalIgnoreCase))
                                    {
                                        machineName = await subReader.ReadElementContentAsStringAsync();
                                        continue;
                                    }

                                    if (string.Equals(subNodeName, "Description", StringComparison.OrdinalIgnoreCase))
                                    {
                                        description = await subReader.ReadElementContentAsStringAsync();
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(machineName) && !uniqueMachines.ContainsKey(machineName))
                        {
                            uniqueMachines.Add(machineName, new MachineInfo
                            {
                                MachineName = machineName,
                                Description = description ?? string.Empty
                            });
                        }
                    }
                }

                filesProcessedCount++;
                var currentProgress = (int)((double)filesProcessedCount / totalFiles * xmlProcessingWeight);
                progress.Report(currentProgress);

                if (filesProcessedCount % 5 == 0 || filesProcessedCount == totalFiles)
                {
                    logService.Log($"Processed {filesProcessedCount} of {totalFiles} files.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logService.LogError($"Error processing file {Path.GetFileName(inputFilePath)}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"Error processing file {inputFilePath}");
                filesProcessedCount++;
                var currentProgress = (int)((double)filesProcessedCount / totalFiles * xmlProcessingWeight);
                progress.Report(currentProgress);
            }
        }

        if (uniqueMachines.Count == 0)
        {
            logService.LogWarning("No valid data found in input files after merging. Operation cancelled.");
            progress.Report(100);
            return;
        }

        try
        {
            logService.Log($"Saving merged XML file with {uniqueMachines.Count} unique entries...");
            cancellationToken.ThrowIfCancellationRequested();

            var writerSettings = new XmlWriterSettings { Indent = true, Async = true };
            await using (var writer = XmlWriter.Create(xmlOutputPath, writerSettings))
            {
                await writer.WriteStartDocumentAsync();
                await writer.WriteStartElementAsync(null, "Machines", null);

                foreach (var machine in uniqueMachines.Values)
                {
                    await writer.WriteStartElementAsync(null, "Machine", null);
                    await writer.WriteElementStringAsync(null, "MachineName", null, machine.MachineName);
                    await writer.WriteElementStringAsync(null, "Description", null, machine.Description);
                    await writer.WriteEndElementAsync();
                }

                await writer.WriteEndElementAsync();
                await writer.WriteEndDocumentAsync();
            }

            logService.Log($"Merged XML saved successfully to: {xmlOutputPath}");
            progress.Report((int)xmlProcessingWeight + (int)(savingWeight * 0.5));

            logService.Log("Converting to MessagePack format...");
            var machinesList = uniqueMachines.Values.ToList();
            cancellationToken.ThrowIfCancellationRequested();
            await SaveMachinesToDatAsync(machinesList, datOutputPath, logService, cancellationToken);
            logService.Log($"Merged DAT file saved successfully to: {datOutputPath}");
            progress.Report(100);
        }
        catch (Exception ex)
        {
            logService.LogError($"Error saving merged files: {ex.Message}");
            await logService.LogExceptionAsync(ex, "Error saving merged files");
            throw;
        }
    }

    private static async Task SaveMachinesToDatAsync(List<MachineInfo> machines, string outputFilePath, ILogService logService, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var binary = await Task.Run(() => MessagePackSerializer.Serialize(machines, cancellationToken: cancellationToken), cancellationToken);
            await File.WriteAllBytesAsync(outputFilePath, binary, cancellationToken);
        }
        catch (Exception ex)
        {
            logService.LogError($"Error saving DAT file: {ex.Message}");
            await logService.LogExceptionAsync(ex, $"Error saving DAT file to {outputFilePath}");
            throw;
        }
    }
}
