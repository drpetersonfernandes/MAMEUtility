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
            var yearData = new Dictionary<string, List<(string Name, string Description)>>(StringComparer.OrdinalIgnoreCase);
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, Async = true };

            // Single pass: collect all required data grouped by year
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
                        string? year = null;
                        string? description = null;

                        using (var subReader = reader.ReadSubtree())
                        {
                            while (await subReader.ReadAsync())
                            {
                                if (subReader.NodeType == XmlNodeType.Element)
                                {
                                    switch (subReader.Name)
                                    {
                                        case "year":
                                            year = await subReader.ReadElementContentAsStringAsync();
                                            break;
                                        case "description":
                                            description = await subReader.ReadElementContentAsStringAsync();
                                            break;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(year) && IsValidYear(year))
                        {
                            if (!yearData.TryGetValue(year, out var machines))
                            {
                                machines = new List<(string Name, string Description)>();
                                yearData[year] = machines;
                            }

                            machines.Add((name, description ?? string.Empty));
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

            logService.Log($"Found {yearData.Count} unique years. Saving files...");

            // Second phase: Write the collected data to files
            var totalYears = yearData.Count;
            var yearsSaved = 0;
            var writerSettings = new XmlWriterSettings { Indent = true, Async = true };

            foreach (var kvp in yearData)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var year = kvp.Key;
                var machines = kvp.Value;
                var safeYear = year.Replace("?", "X");
                var outputFilePath = Path.Combine(outputFolderMameYear, $"{safeYear}.xml");

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

                yearsSaved++;
                var currentProgress = 50 + (int)((double)yearsSaved / totalYears * 50);
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

    private static bool IsValidYear(string year)
    {
        // Allow 4-digit years (e.g., "1980", "1995", "2000")
        // Also allow years with ? for unknown digits (e.g., "198?", "19??")
        if (string.IsNullOrWhiteSpace(year) || year.Length != 4)
            return false;

        // Check if it's all digits or contains only digits and ?
        foreach (var c in year)
        {
            if (!char.IsDigit(c) && c != '?')
                return false;
        }

        return true;
    }
}
