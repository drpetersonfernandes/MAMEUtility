using System.IO;
using System.Xml.Linq;
using MAMEUtility;
using MessagePack;

namespace MameUtility;

public static class MergeList
{
    // Save as both XML and DAT files
    public static void MergeAndSaveBoth(string[] inputFilePaths, string xmlOutputPath, string datOutputPath, LogWindow logWindow)
    {
        if (inputFilePaths.Length == 0)
        {
            logWindow.AppendLog("No input files provided. Operation cancelled.");
            return;
        }

        logWindow.AppendLog($"Starting merge operation with {inputFilePaths.Length} files.");

        // Use a more reliable approach - process files one by one
        XDocument mergedDoc = new(new XElement("Machines"));
        var filesProcessed = 0;
        var totalFiles = inputFilePaths.Length;

        // Define logging intervals
        const int logInterval = 5; // Log every 5 files

        foreach (var inputFilePath in inputFilePaths)
        {
            try
            {
                var inputDoc = XDocument.Load(inputFilePath);

                // Validate and normalize the document structure before merging
                if (!IsValidAndNormalizeStructure(inputDoc, out var normalizedRoot))
                {
                    logWindow.AppendLog($"The file {Path.GetFileName(inputFilePath)} does not have the correct XML structure and will be skipped.");
                    continue;
                }

                // Merge normalized content if we have any
                if (normalizedRoot != null && mergedDoc.Root != null)
                {
                    mergedDoc.Root.Add(normalizedRoot.Elements());
                }

                filesProcessed++;

                // Log at intervals
                if (filesProcessed % logInterval == 0 || filesProcessed == totalFiles)
                {
                    logWindow.AppendLog($"Processed {filesProcessed} of {totalFiles} files.");
                }
            }
            catch (Exception ex)
            {
                logWindow.AppendLog($"Error processing file {Path.GetFileName(inputFilePath)}: {ex.Message}");
                _ = LogError.LogAsync(ex, $"Error processing file {inputFilePath}");
            }
        }

        // Check if we have any elements in the merged document
        if (mergedDoc.Root == null || !mergedDoc.Root.Elements().Any())
        {
            logWindow.AppendLog("No valid data found in input files. Operation cancelled.");
            return;
        }

        try
        {
            // Save as XML
            logWindow.AppendLog("Saving merged XML file...");
            mergedDoc.Save(xmlOutputPath);
            logWindow.AppendLog($"Merged XML saved successfully to: {xmlOutputPath}");

            // Save as MessagePack DAT file
            logWindow.AppendLog("Converting to MessagePack format...");
            var machines = ConvertXmlToMachines(mergedDoc);
            SaveMachinesToDat(machines, datOutputPath, logWindow);
            logWindow.AppendLog($"Merged DAT file saved successfully to: {datOutputPath}");
        }
        catch (Exception ex)
        {
            logWindow.AppendLog($"Error saving merged files: {ex.Message}");
            _ = LogError.LogAsync(ex, "Error saving merged files");
        }
    }

    // Helper method to check if a document has valid structure
    private static bool IsValidAndNormalizeStructure(XDocument doc, out XElement? normalizedRoot)
    {
        normalizedRoot = null;

        // Check root element exists
        if (doc.Root == null)
        {
            return false;
        }

        switch (doc.Root.Name.LocalName)
        {
            // Check for Machines format
            case "Machines" when doc.Root.Elements("Machine").Any():
                normalizedRoot = doc.Root;
                return true;
            // Check for Softwares format
            case "Softwares" when doc.Root.Elements("Software").Any():
            {
                // Normalize Softwares to Machines format
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
                // Invalid structure
                return false;
        }
    }

    // Convert XML to a list of machines compatible with SimpleLauncher's MameConfig
    private static List<MachineInfo> ConvertXmlToMachines(XDocument doc)
    {
        var machines = new List<MachineInfo>();

        if (doc.Root == null)
        {
            return machines;
        }

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

    // Save machines to MessagePack DAT file
    private static void SaveMachinesToDat(List<MachineInfo> machines, string outputFilePath, LogWindow logWindow)
    {
        try
        {
            // Serialize the machines' list to a MessagePack binary array
            var binary = MessagePackSerializer.Serialize(machines);

            // Write the binary data to the output file
            File.WriteAllBytes(outputFilePath, binary);
        }
        catch (Exception ex)
        {
            logWindow.AppendLog($"Error saving DAT file: {ex.Message}");
            _ = LogError.LogAsync(ex, $"Error saving DAT file to {outputFilePath}");
        }
    }
}

// This class matches the structure in SimpleLauncher.MameConfig
[MessagePackObject]
public class MachineInfo
{
    [Key(0)]
    public string MachineName { get; set; } = string.Empty;

    [Key(1)]
    public string Description { get; set; } = string.Empty;
}