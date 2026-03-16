using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameYear
{
    public static async Task CreateAndSaveMameYearAsync(string inputFilePath, string outputFolderMameYear, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        logService.Log($"Processing years from: {inputFilePath}");
        logService.Log($"Output folder: {outputFolderMameYear}");

        try
        {
            // First pass: collect unique years
            var years = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                        string? year = null;
                        using (var subReader = reader.ReadSubtree())
                        {
                            while (await subReader.ReadAsync())
                            {
                                if (subReader is { NodeType: XmlNodeType.Element, Name: "year" })
                                {
                                    year = await subReader.ReadElementContentAsStringAsync();
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(year))
                        {
                            years.Add(year);
                        }

                        processedCount++;
                        if (processedCount % 10000 == 0)
                        {
                            logService.Log($"Scanned {processedCount} machines for years...");
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
            logService.Log($"Found {years.Count} unique years. Processing each...");

            // Second pass: for each year, stream through file and write matches
            var totalYears = years.Count;
            var yearsSaved = 0;
            var writerSettings = new XmlWriterSettings { Indent = true, Async = true };

            foreach (var year in years)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var safeYear = year.Replace("?", "X");
                var outputFilePath = Path.Combine(outputFolderMameYear, $"{safeYear}.xml");

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
                            string? machineYear = null;
                            string? description = null;

                            using (var subReader = readReader.ReadSubtree())
                            {
                                while (await subReader.ReadAsync())
                                {
                                    if (subReader.NodeType == XmlNodeType.Element)
                                    {
                                        switch (subReader.Name)
                                        {
                                            case "year":
                                                machineYear = await subReader.ReadElementContentAsStringAsync();
                                                break;
                                            case "description":
                                                description = await subReader.ReadElementContentAsStringAsync();
                                                break;
                                        }
                                    }
                                }
                            }

                            if (string.Equals(machineYear, year, StringComparison.OrdinalIgnoreCase))
                            {
                                await writer.WriteStartElementAsync(null, "Machine", null);
                                await writer.WriteElementStringAsync(null, "MachineName", null, name);
                                await writer.WriteElementStringAsync(null, "Description", null, description ?? string.Empty);
                                await writer.WriteEndElementAsync();
                            }
                        }
                    }

                    await writer.WriteEndElementAsync();
                    await writer.WriteEndDocumentAsync();
                }

                yearsSaved++;
                var currentProgress = 25 + (int)((double)yearsSaved / totalYears * 75);
                progress.Report(currentProgress);

                if (yearsSaved % 10 == 0 || yearsSaved == totalYears)
                {
                    logService.Log($"Saved {yearsSaved}/{totalYears} year files.");
                }
            }

            progress.Report(100);
            logService.Log("All year files created successfully.");
        }
        catch (Exception ex)
        {
            await logService.LogExceptionAsync(ex, "Error in method MAMEYear.CreateAndSaveMameYearAsync");
            throw;
        }
    }
}
