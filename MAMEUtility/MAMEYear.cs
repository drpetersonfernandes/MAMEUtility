using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameYear
{
    public static async Task CreateAndSaveMameYearAsync(XDocument inputDoc, string outputFolderMameYear, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        logService.Log($"Output folder for MAME Year: {outputFolderMameYear}");

        try
        {
            progress.Report(5);

            logService.Log("Extracting years from XML...");
            var years = await Task.Run(() =>
                inputDoc.Descendants("machine")
                    .Select(static m => (string?)m.Element("year"))
                    .Distinct()
                    .Where(static y => !string.IsNullOrEmpty(y))
                    .ToList(), cancellationToken);

            progress.Report(10);

            var totalYears = years.Count;
            logService.Log($"Found {totalYears} unique years to process.");

            // Thread-safe collection for parallel processing
            var yearDocs = new ConcurrentDictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
            var processedCount = 0;

            var logInterval = Math.Max(1, totalYears / 10);

            // Parallel processing of years
            logService.Log("Processing years in parallel...");

            await Task.Run(() =>
            {
                Parallel.ForEach(years, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                }, year =>
                {
                    if (year == null) return;

                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var machinesForYear = inputDoc.Descendants("machine")
                            .Where(m => (string?)m.Element("year") == year)
                            .ToList();

                        XDocument yearDoc = new(
                            new XElement("Machines",
                                from machine in machinesForYear
                                select new XElement("Machine",
                                    new XElement("MachineName", machine.Attribute("name")?.Value ?? ""),
                                    new XElement("Description", machine.Element("description")?.Value ?? "")
                                )
                            )
                        );

                        // Use a safe key (replace '?' with 'X')
                        var safeYear = year.Replace("?", "X");
                        yearDocs[safeYear] = yearDoc;

                        var currentCount = Interlocked.Increment(ref processedCount);

                        if (currentCount % logInterval != 0 && currentCount != totalYears) return;

                        logService.Log($"Processing progress: {currentCount}/{totalYears} years");
                        var progressPercentage = 10 + (int)((double)currentCount / totalYears * 70);
                        progress.Report(progressPercentage);
                    }
                    catch (Exception ex)
                    {
                        logService.LogError($"Failed to process year '{year}': {ex.Message}");
                        _ = logService.LogExceptionAsync(ex, $"Error processing year '{year}'");
                    }
                });
            }, cancellationToken);

            progress.Report(80);

            // Save all collected documents to disk
            logService.Log("Saving year XML files...");
            var savedCount = 0;
            var totalToSave = yearDocs.Count;

            foreach (var kvp in yearDocs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var safeYear = kvp.Key;
                var yearDoc = kvp.Value;
                var outputFilePath = Path.Combine(outputFolderMameYear, $"{safeYear}.xml");

                try
                {
                    await Task.Run(() => yearDoc.Save(outputFilePath), cancellationToken);
                }
                catch (Exception ex)
                {
                    logService.LogError($"Failed to save file for year '{safeYear}': {ex.Message}");
                    await logService.LogExceptionAsync(ex, $"Error saving year document for '{safeYear}'");
                }

                savedCount++;
                if (savedCount % 10 != 0 && savedCount != totalToSave) continue;

                var saveProgress = 80 + (int)((double)savedCount / totalToSave * 20);
                progress.Report(saveProgress);
            }

            progress.Report(100);
            logService.Log("All year files created successfully.");
        }
        catch (Exception ex)
        {
            logService.LogError("An error occurred: " + ex.Message);
            await logService.LogExceptionAsync(ex, "Error in method MAMEYear.CreateAndSaveMameYearAsync");
            throw;
        }
    }
}
