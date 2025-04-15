using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MAMEUtility;

public partial class MameManufacturer
{
    public static async Task CreateAndSaveMameManufacturerAsync(XDocument inputDoc, string outputFolderMameManufacturer, IProgress<int> progress, LogWindow logWindow)
    {
        logWindow.AppendLog($"Output folder for MAME Manufacturer: {outputFolderMameManufacturer}");

        try
        {
            // Extract unique manufacturers - ensure true uniqueness
            var manufacturers = inputDoc.Descendants("machine")
                .Select(static m => (string?)m.Element("manufacturer"))
                .Where(static m => !string.IsNullOrEmpty(m))
                .Distinct(StringComparer.OrdinalIgnoreCase) // Use case-insensitive comparison
                .ToList();

            var totalManufacturers = manufacturers.Count;
            var manufacturersProcessed = 0;

            // Log less frequently - set interval
            const int logInterval = 10; // Log every 10 manufacturers
            const int progressInterval = 5; // Update progress every 5 manufacturers

            logWindow.AppendLog($"Found {totalManufacturers} unique manufacturers to process.");

            // Use a dictionary to track processed manufacturer names and their safe filenames
            var processedManufacturers = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Use a dictionary approach instead of parallel processing to avoid file conflicts
            foreach (var manufacturer in manufacturers)
            {
                if (manufacturer == null) continue;

                try
                {
                    var safeManufacturerName = RemoveExtraWhitespace(manufacturer
                            .Replace("<", "")
                            .Replace(">", "")
                            .Replace(":", "")
                            .Replace("\"", "")
                            .Replace("/", "")
                            .Replace("\\", "")
                            .Replace("|", "")
                            .Replace("?", "")
                            .Replace("*", "")
                            .Replace("unknown", "UnknownManufacturer")
                            .Trim())
                        .Replace("&amp;", "&");

                    // Check if this manufacturer name has already been processed
                    // If so, append a number to make it unique
                    var uniqueSafeName = safeManufacturerName;
                    var counter = 1;
                    while (!processedManufacturers.TryAdd(manufacturer, uniqueSafeName))
                    {
                        uniqueSafeName = $"{safeManufacturerName}_{counter++}";
                    }

                    var outputFilePath = Path.Combine(outputFolderMameManufacturer, $"{uniqueSafeName}.xml");

                    // Create filtered document for this manufacturer
                    var filteredDoc = CreateFilteredDocument(inputDoc, manufacturer);

                    // Save to file with exclusive access
                    await using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await Task.Run(() => filteredDoc.Save(fileStream));
                    }

                    // Thread-safe incrementing
                    var processed = Interlocked.Increment(ref manufacturersProcessed);

                    // Log only at intervals
                    if (processed % logInterval == 0 || processed == totalManufacturers)
                    {
                        logWindow.AppendLog($"Progress: {processed}/{totalManufacturers} manufacturers processed.");
                    }

                    // Update progress less frequently
                    if (processed % progressInterval == 0 || processed == totalManufacturers)
                    {
                        var progressPercentage = (double)processed / totalManufacturers * 100;
                        progress.Report((int)progressPercentage);
                    }
                }
                catch (IOException ex)
                {
                    // Log the file access error but continue processing other manufacturers
                    logWindow.AppendLog($"File access error processing manufacturer '{manufacturer}': {ex.Message}");
                    await LogError.LogAsync(ex, $"File access error processing manufacturer '{manufacturer}'");
                }
                catch (Exception ex)
                {
                    // Log other errors but continue processing
                    logWindow.AppendLog($"Error processing manufacturer '{manufacturer}': {ex.Message}");
                    await LogError.LogAsync(ex, $"Error processing manufacturer '{manufacturer}'");
                }
            }

            logWindow.AppendLog("All manufacturer files created successfully.");
        }
        catch (Exception ex)
        {
            logWindow.AppendLog("An error occurred: " + ex.Message);
            await LogError.LogAsync(ex, "Error in method MAMEManufacturer.CreateAndSaveMameManufacturerAsync");
        }
    }

    private static XDocument CreateFilteredDocument(XContainer inputDoc, string manufacturer)
    {
        // Retrieve the matched machines
        var matchedMachines = inputDoc.Descendants("machine").Where(Predicate).ToList();

        // Create a new XML document for machines based on the matched machines
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

        bool Predicate(XElement machine)
        {
            return (machine.Element("manufacturer")?.Value ?? "") == manufacturer &&
                   //!(machine.Attribute("name")?.Value.Contains("bootleg", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                   //!(machine.Element("description")?.Value.Contains("bootleg", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                   //!(machine.Element("description")?.Value.Contains("prototype", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                   //!(machine.Element("description")?.Value.Contains("playchoice", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                   //machine.Attribute("cloneof") == null &&
                   !(machine.Attribute("name")?.Value.Contains("bios", StringComparison.InvariantCultureIgnoreCase) ??
                     false) &&
                   !(machine.Element("description")?.Value
                       .Contains("bios", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
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