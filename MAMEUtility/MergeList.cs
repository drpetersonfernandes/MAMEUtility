using System.IO;
using System.Xml.Linq;
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
            progress.Report(100); // Indicate completion for no-op
            return;
        }

        logService.Log($"Starting merge operation with {inputFilePaths.Length} files.");
        progress.Report(0); // Initial progress

        // Use a dictionary to store unique machines by MachineName (case-insensitive)
        // This automatically handles duplicates by keeping the first encountered entry.
        var uniqueMachines = new Dictionary<string, MachineInfo>(StringComparer.OrdinalIgnoreCase);
        var filesProcessed = 0;
        var totalFiles = inputFilePaths.Length;

        const int logInterval = 5;
        const double xmlProcessingWeight = 80.0; // Percentage of progress for XML processing
        const double savingWeight = 20.0; // Percentage of progress for saving

        foreach (var inputFilePath in inputFilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath), cancellationToken);

                int currentProgress;
                if (!IsValidAndNormalizeStructure(inputDoc, out var normalizedRoot))
                {
                    logService.LogWarning($"The file {Path.GetFileName(inputFilePath)} does not have the correct XML structure and will be skipped.");
                    // Still increment filesProcessed to ensure overall progress calculation is accurate
                    filesProcessed++;
                    currentProgress = (int)((double)filesProcessed / totalFiles * xmlProcessingWeight);
                    progress.Report(currentProgress);
                    continue;
                }

                if (normalizedRoot != null)
                {
                    foreach (var machineElement in normalizedRoot.Elements("Machine"))
                    {
                        var machineName = machineElement.Element("MachineName")?.Value;
                        if (!string.IsNullOrEmpty(machineName))
                        {
                            // Add if not already present. If duplicate, the existing one is kept.
                            if (!uniqueMachines.ContainsKey(machineName))
                            {
                                var machine = new MachineInfo
                                {
                                    MachineName = machineName,
                                    Description = machineElement.Element("Description")?.Value ?? string.Empty
                                };
                                uniqueMachines.Add(machineName, machine);
                            }
                            else
                            {
                                // Optionally log that a duplicate was skipped
                                // logService.Log($"Skipping duplicate machine entry: {machineName} from {Path.GetFileName(inputFilePath)}");
                            }
                        }
                    }
                }

                filesProcessed++;

                // Report progress for XML processing phase
                currentProgress = (int)((double)filesProcessed / totalFiles * xmlProcessingWeight);
                progress.Report(currentProgress);

                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logService.Log($"Processed {filesProcessed} of {totalFiles} files.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logService.LogError($"Error processing file {Path.GetFileName(inputFilePath)}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"Error processing file {inputFilePath}");
                // Increment filesProcessed even on error to ensure progress bar eventually completes
                filesProcessed++;
                var currentProgress = (int)((double)filesProcessed / totalFiles * xmlProcessingWeight);
                progress.Report(currentProgress);
            }
        }

        if (uniqueMachines.Count == 0)
        {
            logService.LogWarning("No valid data found in input files after merging. Operation cancelled.");
            progress.Report(100); // Indicate completion for no-op
            return;
        }

        // Create the final merged XDocument from unique machines
        XDocument mergedDoc = new(new XElement("Machines",
            uniqueMachines.Values.Select(m => new XElement("Machine",
                new XElement("MachineName", m.MachineName),
                new XElement("Description", m.Description)
            ))
        ));

        try
        {
            logService.Log($"Saving merged XML file with {uniqueMachines.Count} unique entries...");
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() => mergedDoc.Save(xmlOutputPath), cancellationToken);
            logService.Log($"Merged XML saved successfully to: {xmlOutputPath}");
            // Report progress after XML save, before DAT save
            progress.Report((int)xmlProcessingWeight + (int)(savingWeight * 0.5));

            logService.Log("Converting to MessagePack format...");
            var machinesList = uniqueMachines.Values.ToList(); // Convert dictionary values to list
            cancellationToken.ThrowIfCancellationRequested();
            await SaveMachinesToDatAsync(machinesList, datOutputPath, logService, cancellationToken);
            logService.Log($"Merged DAT file saved successfully to: {datOutputPath}");
            progress.Report(100); // Final 100%
        }
        catch (Exception ex)
        {
            logService.LogError($"Error saving merged files: {ex.Message}");
            await logService.LogExceptionAsync(ex, "Error saving merged files");
            throw;
        }
    }

    private static bool IsValidAndNormalizeStructure(XDocument doc, out XElement? normalizedRoot)
    {
        normalizedRoot = null;

        if (doc.Root == null)
        {
            return false;
        }

        switch (doc.Root.Name.LocalName)
        {
            case "Machines" when doc.Root.Elements("Machine").Any():
                normalizedRoot = doc.Root;
                return true;
            case "Softwares" when doc.Root.Elements("Software").Any():
            {
                normalizedRoot = new XElement("Machines");
                foreach (var software in doc.Root.Elements("Software"))
                {
                    var machineElement = new XElement("Machine");
                    var softwareName = software.Element("SoftwareName")?.Value;
                    if (!string.IsNullOrEmpty(softwareName))
                    {
                        machineElement.Add(new XElement("MachineName", softwareName));
                    }

                    var description = software.Element("Description");
                    if (description != null)
                    {
                        machineElement.Add(new XElement("Description", description.Value));
                    }

                    normalizedRoot.Add(machineElement);
                }

                return normalizedRoot.Elements().Any();
            }
            default:
                return false;
        }
    }

    private static async Task SaveMachinesToDatAsync(List<MachineInfo> machines, string outputFilePath, ILogService logService, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var binary = await Task.Run(() => MessagePackSerializer.Serialize(machines), cancellationToken);
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