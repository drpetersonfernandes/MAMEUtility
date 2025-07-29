using System.IO;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;
using MessagePack;

namespace MAMEUtility;

public static class MergeList
{
    public static async Task MergeAndSaveBothAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath, ILogService logService)
    {
        if (inputFilePaths.Length == 0)
        {
            logService.LogWarning("No input files provided. Operation cancelled.");
            return;
        }

        logService.Log($"Starting merge operation with {inputFilePaths.Length} files.");

        XDocument mergedDoc = new(new XElement("Machines"));
        var filesProcessed = 0;
        var totalFiles = inputFilePaths.Length;

        const int logInterval = 5;

        foreach (var inputFilePath in inputFilePaths)
        {
            try
            {
                var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

                if (!IsValidAndNormalizeStructure(inputDoc, out var normalizedRoot))
                {
                    logService.LogWarning($"The file {Path.GetFileName(inputFilePath)} does not have the correct XML structure and will be skipped.");
                    continue;
                }

                if (normalizedRoot != null && mergedDoc.Root != null)
                {
                    mergedDoc.Root.Add(normalizedRoot.Elements());
                }

                filesProcessed++;

                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logService.Log($"Processed {filesProcessed} of {totalFiles} files.");
                }
            }
            catch (Exception ex)
            {
                logService.LogError($"Error processing file {Path.GetFileName(inputFilePath)}: {ex.Message}");
                await logService.LogExceptionAsync(ex, $"Error processing file {inputFilePath}");
            }
        }

        if (mergedDoc.Root == null || !mergedDoc.Root.Elements().Any())
        {
            logService.LogWarning("No valid data found in input files. Operation cancelled.");
            return;
        }

        try
        {
            logService.Log("Saving merged XML file...");
            await Task.Run(() => mergedDoc.Save(xmlOutputPath));
            logService.Log($"Merged XML saved successfully to: {xmlOutputPath}");

            logService.Log("Converting to MessagePack format...");
            var machines = ConvertXmlToMachines(mergedDoc);
            await SaveMachinesToDatAsync(machines, datOutputPath, logService);
            logService.Log($"Merged DAT file saved successfully to: {datOutputPath}");
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

    private static List<MachineInfo> ConvertXmlToMachines(XDocument doc)
    {
        var machines = new List<MachineInfo>();
        if (doc.Root == null) return machines;

        foreach (var machineElement in doc.Root.Elements("Machine"))
        {
            var machine = new MachineInfo
            {
                MachineName = machineElement.Element("MachineName")?.Value ?? string.Empty,
                Description = machineElement.Element("Description")?.Value ?? string.Empty
            };
            machines.Add(machine);
        }

        return machines;
    }

    private static async Task SaveMachinesToDatAsync(List<MachineInfo> machines, string outputFilePath, ILogService logService)
    {
        try
        {
            var binary = await Task.Run(() => MessagePackSerializer.Serialize(machines));
            await File.WriteAllBytesAsync(outputFilePath, binary);
        }
        catch (Exception ex)
        {
            logService.LogError($"Error saving DAT file: {ex.Message}");
            await logService.LogExceptionAsync(ex, $"Error saving DAT file to {outputFilePath}");
            throw;
        }
    }
}

[MessagePackObject]
public class MachineInfo
{
    [Key(0)]
    public string MachineName { get; set; } = string.Empty;

    [Key(1)]
    public string Description { get; set; } = string.Empty;
}
