using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameYear
{
    public static async Task CreateAndSaveMameYearAsync(
        string inputFilePath,
        string outputFolderMameYear,
        IProgress<int> progress,
        ILogService logService,
        CancellationToken cancellationToken = default)
    {
        logService.Log($"Processing years from: {inputFilePath}");
        logService.Log($"Output folder: {outputFolderMameYear}");

        try
        {
            var yearData = new Dictionary<string, List<(string Name, string Description)>>(StringComparer.OrdinalIgnoreCase);
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreWhitespace = true,
                Async = true
            };

            // Single pass: collect all data grouped by year
            await using (var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                var totalBytes = fileStream.Length;
                var lastReportedProgress = -1;
                long processedCount = 0;

                using var reader = XmlReader.Create(fileStream, readerSettings);

                while (await reader.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element &&
                        string.Equals(reader.Name, "machine", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = reader.GetAttribute("name") ?? "";
                        string? year = null;
                        string? description = null;

                        // Use ReadSubtree() - this is the correct pattern used in other files
                        using var subReader = reader.ReadSubtree();
                        while (await subReader.ReadAsync())
                        {
                            if (subReader.NodeType == XmlNodeType.Element)
                            {
                                if (string.Equals(subReader.Name, "year", StringComparison.OrdinalIgnoreCase))
                                {
                                    year = (await subReader.ReadElementContentAsStringAsync()).Trim();
                                }
                                else if (string.Equals(subReader.Name, "description", StringComparison.OrdinalIgnoreCase))
                                {
                                    description = await subReader.ReadElementContentAsStringAsync();
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

                    // Report progress based on bytes read
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

            logService.Log($"Found {yearData.Count} unique years with {yearData.Values.Sum(static v => v.Count)} total machines.");

            if (yearData.Count == 0)
            {
                logService.LogWarning("No valid year data was found. Check if the XML contains <year> tags.");
                progress.Report(100);
                return;
            }

            logService.Log("Saving year XML files...");

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

                if (yearsSaved % 20 == 0 || yearsSaved == totalYears)
                {
                    logService.Log($"Saved {yearsSaved}/{totalYears} year files.");
                }
            }

            progress.Report(100);
            logService.Log("All year files created successfully.");
        }
        catch (Exception ex)
        {
            await logService.LogExceptionAsync(ex, "Error in MameYear.CreateAndSaveMameYearAsync");
            throw;
        }
    }

    private static bool IsValidYear(string year)
    {
        year = year.Trim();
        if (string.IsNullOrWhiteSpace(year) || year.Length != 4)
            return false;

        foreach (var c in year)
        {
            if (!char.IsDigit(c) && c != '?')
                return false;
        }

        return true;
    }
}
