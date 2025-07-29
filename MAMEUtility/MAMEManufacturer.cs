using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

public partial class MameManufacturer
{
    public static async Task CreateAndSaveMameManufacturerAsync(XDocument inputDoc, string outputFolderMameManufacturer, IProgress<int> progress, ILogService logService)
    {
        logService.Log($"Output folder for MAME Manufacturer: {outputFolderMameManufacturer}");

        try
        {
            // Initial progress update
            progress.Report(5);

            logService.Log("Extracting manufacturers from XML...");
            var manufacturers = await Task.Run(() =>
                inputDoc.Descendants("machine")
                    .Select(static m => (string?)m.Element("manufacturer"))
                    .Where(static m => !string.IsNullOrEmpty(m))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            );

            progress.Report(10);

            var totalManufacturers = manufacturers.Count;
            logService.Log($"Found {totalManufacturers} unique manufacturers to process.");

            // Thread-safe collections for parallel processing
            var manufacturerDocs = new ConcurrentDictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
            var generatedFileNames = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var processedCount = 0;

            var logInterval = Math.Max(1, totalManufacturers / 10);

            // Parallel processing of manufacturers
            logService.Log("Processing manufacturers in parallel...");

            await Task.Run(() =>
            {
                Parallel.ForEach(manufacturers, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }, manufacturer =>
                {
                    if (manufacturer == null) return;

                    try
                    {
                        var safeManufacturerName = RemoveExtraWhitespace(manufacturer
                                .Replace("<", "").Replace(">", "").Replace(":", "").Replace("\"", "")
                                .Replace("/", "").Replace("\\", "").Replace("|", "").Replace("?", "")
                                .Replace("*", "").Replace("unknown", "UnknownManufacturer").Trim())
                            .Replace("&amp;", "&");

                        var uniqueSafeName = safeManufacturerName;
                        var counter = 1;
                        while (!generatedFileNames.TryAdd(uniqueSafeName, true))
                        {
                            uniqueSafeName = $"{safeManufacturerName}_{counter++}";
                        }

                        // Create the filtered document
                        var filteredDoc = CreateFilteredDocument(inputDoc, manufacturer);
                        manufacturerDocs[uniqueSafeName] = filteredDoc;

                        var currentCount = Interlocked.Increment(ref processedCount);

                        if (currentCount % logInterval != 0 && currentCount != totalManufacturers) return;

                        logService.Log($"Processing progress: {currentCount}/{totalManufacturers} manufacturers");
                        var progressPercentage = 10 + (int)((double)currentCount / totalManufacturers * 70);
                        progress.Report(progressPercentage);
                    }
                    catch (Exception ex)
                    {
                        logService.LogError($"Error processing manufacturer '{manufacturer}': {ex.Message}");
                        _ = logService.LogExceptionAsync(ex, $"Error processing manufacturer '{manufacturer}'");
                    }
                });
            });

            progress.Report(80);

            // Save all collected documents to disk
            logService.Log("Saving manufacturer XML files...");
            var savedCount = 0;
            var totalToSave = manufacturerDocs.Count;

            foreach (var kvp in manufacturerDocs)
            {
                var uniqueSafeName = kvp.Key;
                var filteredDoc = kvp.Value;
                var outputFilePath = Path.Combine(outputFolderMameManufacturer, $"{uniqueSafeName}.xml");

                try
                {
                    // Skip saving if no machines
                    if (filteredDoc.Root == null || !filteredDoc.Root.Elements("Machine").Any())
                    {
                        logService.Log($"Skipping empty manufacturer file: {uniqueSafeName} (0 machines)");
                        continue;
                    }

                    await Task.Run(() => filteredDoc.Save(outputFilePath));
                    logService.Log($"Saved manufacturer file: {uniqueSafeName} with {filteredDoc.Root.Elements("Machine").Count()} machines");
                }
                catch (Exception ex)
                {
                    logService.LogError($"Failed to save file for manufacturer '{uniqueSafeName}': {ex.Message}");
                    await logService.LogExceptionAsync(ex, $"Error saving manufacturer document for '{uniqueSafeName}'");
                }

                savedCount++;
                if (savedCount % 10 != 0 && savedCount != totalToSave) continue;

                var saveProgress = 80 + (int)((double)savedCount / totalToSave * 20);
                progress.Report(saveProgress);
            }

            progress.Report(100);
            logService.Log("All manufacturer files created successfully.");
        }
        catch (Exception ex)
        {
            logService.LogError("An error occurred: " + ex.Message);
            await logService.LogExceptionAsync(ex, "Error in method MAMEManufacturer.CreateAndSaveMameManufacturerAsync");
            throw;
        }
    }

    private static XDocument CreateFilteredDocument(XContainer inputDoc, string manufacturer)
    {
        var matchedMachines = inputDoc.Descendants("machine").Where(m => Predicate(m, manufacturer)).ToList();

        XDocument filteredDoc = new(
            new XElement("Machines",
                from machine in matchedMachines
                select new XElement("Machine",
                    new XElement("MachineName", RemoveExtraWhitespace(machine.Attribute("name")?.Value ?? "").Replace("&amp;", "&")),
                    new XElement("Description", RemoveExtraWhitespace(machine.Element("description")?.Value ?? "").Replace("&amp;", "&"))
                )
            )
        );
        return filteredDoc;

        static bool Predicate(XElement machine, string manufacturer)
        {
            var machineManufacturer = machine.Element("manufacturer")?.Value ?? "";
            return string.Equals(machineManufacturer, manufacturer, StringComparison.OrdinalIgnoreCase) &&
                   !(machine.Attribute("name")?.Value.Contains("bios", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                   !(machine.Element("description")?.Value.Contains("bios", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                   (machine.Element("driver")?.Attribute("emulation")?.Value ?? "") == "good";
        }
    }

    private static string RemoveExtraWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return MyRegex().Replace(input, " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}
