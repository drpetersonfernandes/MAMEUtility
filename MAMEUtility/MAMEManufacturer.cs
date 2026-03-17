using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameManufacturer
{
    public static async Task CreateAndSaveMameManufacturerAsync(string inputFilePath, string outputFolderMameManufacturer, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        logService.Log($"Processing manufacturers from: {inputFilePath}");
        logService.Log($"Output folder: {outputFolderMameManufacturer}");

        try
        {
            var manufacturerData = new Dictionary<string, List<(string Name, string Description)>>(StringComparer.OrdinalIgnoreCase);
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, Async = true };

            // Single pass: collect all required data grouped by manufacturer
            await using (var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                var totalBytes = fileStream.Length;
                var lastReportedProgress = -1;
                long processedCount = 0;

                using var reader = XmlReader.Create(fileStream, readerSettings);
                while (await reader.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader is { NodeType: XmlNodeType.Element, Name: "machine" })
                    {
                        var name = reader.GetAttribute("name") ?? "";
                        var emulation = reader.GetAttribute("emulation") ?? "";

                        if (name.Contains("bios", StringComparison.OrdinalIgnoreCase) || emulation == "preliminary")
                        {
                            reader.Skip();
                            continue;
                        }

                        string? manufacturer = null;
                        string? description = null;

                        using (var subReader = reader.ReadSubtree())
                        {
                            while (await subReader.ReadAsync())
                            {
                                if (subReader.NodeType == XmlNodeType.Element)
                                {
                                    switch (subReader.Name)
                                    {
                                        case "manufacturer":
                                            manufacturer = await subReader.ReadElementContentAsStringAsync();
                                            break;
                                        case "description":
                                            description = await subReader.ReadElementContentAsStringAsync();
                                            break;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(manufacturer) && !string.IsNullOrEmpty(description) && !description.Contains("bios", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!manufacturerData.TryGetValue(manufacturer, out var machines))
                            {
                                machines = new List<(string Name, string Description)>();
                                manufacturerData[manufacturer] = machines;
                            }

                            machines.Add((name, description));
                        }

                        processedCount++;
                        if (processedCount % 10000 == 0)
                        {
                            logService.Log($"Scanned {processedCount} machines...");
                        }
                    }

                    if (totalBytes > 0)
                    {
                        var currentProgress = (int)((double)fileStream.Position / totalBytes * 50);
                        if (currentProgress > lastReportedProgress)
                        {
                            progress.Report(currentProgress);
                            lastReportedProgress = currentProgress;
                        }
                    }
                }
            }

            logService.Log($"Found {manufacturerData.Count} unique manufacturers. Saving files...");

            // Second phase: Write the collected data to files
            var totalManufacturers = manufacturerData.Count;
            var manufacturersSaved = 0;
            var writerSettings = new XmlWriterSettings { Indent = true, Async = true };

            foreach (var kvp in manufacturerData)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var manufacturer = kvp.Key;
                var machines = kvp.Value;
                var safeName = FileNameHelper.SanitizeForFileName(manufacturer);
                var outputFilePath = Path.Combine(outputFolderMameManufacturer, $"{safeName}.xml");

                await using (var writer = XmlWriter.Create(outputFilePath, writerSettings))
                {
                    await writer.WriteStartDocumentAsync();
                    await writer.WriteStartElementAsync(null, "Machines", null);

                    foreach (var machine in machines)
                    {
                        await writer.WriteStartElementAsync(null, "Machine", null);
                        await writer.WriteElementStringAsync(null, "MachineName", null, machine.Name);
                        await writer.WriteElementStringAsync(null, "Description", null, machine.Description);
                        await writer.WriteEndElementAsync();
                    }

                    await writer.WriteEndElementAsync();
                    await writer.WriteEndDocumentAsync();
                }

                manufacturersSaved++;
                var currentProgress = 50 + (int)((double)manufacturersSaved / totalManufacturers * 50);
                progress.Report(currentProgress);

                if (manufacturersSaved % 50 == 0 || manufacturersSaved == totalManufacturers)
                {
                    logService.Log($"Saved {manufacturersSaved}/{totalManufacturers} manufacturer files.");
                }
            }

            progress.Report(100);
            logService.Log("Manufacturer lists generated successfully.");
        }
        catch (Exception ex)
        {
            await logService.LogExceptionAsync(ex, "Error in method MameManufacturer.CreateAndSaveMameManufacturerAsync");
            throw;
        }
    }
}
