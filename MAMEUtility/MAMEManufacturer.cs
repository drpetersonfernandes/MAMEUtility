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
            // First pass: collect unique manufacturers
            var manufacturers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, Async = true };

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
                        using (var subReader = reader.ReadSubtree())
                        {
                            while (await subReader.ReadAsync())
                            {
                                if (subReader is { NodeType: XmlNodeType.Element, Name: "manufacturer" })
                                {
                                    manufacturer = await subReader.ReadElementContentAsStringAsync();
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(manufacturer))
                        {
                            manufacturers.Add(manufacturer);
                        }

                        processedCount++;
                        if (processedCount % 10000 == 0)
                        {
                            logService.Log($"Scanned {processedCount} machines for manufacturers...");
                        }
                    }

                    if (totalBytes > 0)
                    {
                        var currentProgress = (int)((double)fileStream.Position / totalBytes * 25);
                        if (currentProgress > lastReportedProgress)
                        {
                            progress.Report(currentProgress);
                            lastReportedProgress = currentProgress;
                        }
                    }
                }
            }

            progress.Report(25);
            logService.Log($"Found {manufacturers.Count} unique manufacturers. Processing each...");

            // Second pass: for each manufacturer, stream through file and write matches
            var totalManufacturers = manufacturers.Count;
            var manufacturersSaved = 0;
            var writerSettings = new XmlWriterSettings { Indent = true, Async = true };

            foreach (var manufacturer in manufacturers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var safeName = FileNameHelper.SanitizeForFileName(manufacturer);
                var outputFilePath = Path.Combine(outputFolderMameManufacturer, $"{safeName}.xml");

                await using (var writer = XmlWriter.Create(outputFilePath, writerSettings))
                {
                    await writer.WriteStartDocumentAsync();
                    await writer.WriteStartElementAsync(null, "Machines", null);

                    // Stream through input file and write matching machines
                    await using var readStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                    using var readReader = XmlReader.Create(readStream, readerSettings);

                    while (await readReader.ReadAsync())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (readReader is { NodeType: XmlNodeType.Element, Name: "machine" })
                        {
                            var name = readReader.GetAttribute("name") ?? "";
                            var emulation = readReader.GetAttribute("emulation") ?? "";

                            if (name.Contains("bios", StringComparison.OrdinalIgnoreCase) || emulation == "preliminary")
                            {
                                readReader.Skip();
                                continue;
                            }

                            string? machineManufacturer = null;
                            string? description = null;

                            using (var subReader = readReader.ReadSubtree())
                            {
                                while (await subReader.ReadAsync())
                                {
                                    if (subReader.NodeType == XmlNodeType.Element)
                                    {
                                        switch (subReader.Name)
                                        {
                                            case "manufacturer":
                                                machineManufacturer = await subReader.ReadElementContentAsStringAsync();
                                                break;
                                            case "description":
                                                description = await subReader.ReadElementContentAsStringAsync();
                                                break;
                                        }
                                    }
                                }
                            }

                            if (string.Equals(machineManufacturer, manufacturer, StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrEmpty(description) &&
                                !description.Contains("bios", StringComparison.OrdinalIgnoreCase))
                            {
                                await writer.WriteStartElementAsync(null, "Machine", null);
                                await writer.WriteElementStringAsync(null, "MachineName", null, name);
                                await writer.WriteElementStringAsync(null, "Description", null, description);
                                await writer.WriteEndElementAsync();
                            }
                        }
                    }

                    await writer.WriteEndElementAsync();
                    await writer.WriteEndDocumentAsync();
                }

                manufacturersSaved++;
                var currentProgress = 25 + (int)((double)manufacturersSaved / totalManufacturers * 75);
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
