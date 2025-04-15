using System.IO;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces; // Added

namespace MAMEUtility;

public static class MameYear
{
    public static async Task CreateAndSaveMameYearAsync(XDocument inputDoc, string outputFolderMameYear, IProgress<int> progress, ILogService logService)
    {
        logService.Log($"Output folder for MAME Year: {outputFolderMameYear}");

        try
        {
            // Extract unique years and cache the result
            var years = inputDoc.Descendants("machine")
                .Select(static m => (string?)m.Element("year"))
                .Distinct()
                .Where(static y => !string.IsNullOrEmpty(y))
                .ToList();

            var totalYears = years.Count;
            var yearsProcessed = 0;

            // Define logging and progress intervals
            const int logInterval = 5; // Log every 5 years
            const int progressInterval = 2; // Update progress every 2 years

            logService.Log($"Found {totalYears} unique years to process.");

            // Use parallel processing with throttling
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };

            await Parallel.ForEachAsync(years, options, async (year, token) =>
            {
                if (year == null) return;

                // Filter machines based on year - do this within the parallel operation
                var machinesForYear = inputDoc.Descendants("machine")
                    .Where(m => (string?)m.Element("year") == year)
                    .ToList(); // Cache the result to avoid repeated queries

                // Create the XML document for the year
                XDocument yearDoc = new(
                    new XElement("Machines",
                        from machine in machinesForYear
                        select new XElement("Machine",
                            new XElement("MachineName", machine.Attribute("name")?.Value ?? ""),
                            new XElement("Description", machine.Element("description")?.Value ?? "")
                        )
                    )
                );

                // Save the XML document for the year
                var outputFilePath = Path.Combine(outputFolderMameYear, $"{year.Replace("?", "X")}.xml");
                try
                {
                    await Task.Run(() => yearDoc.Save(outputFilePath), token);
                }
                catch (Exception ex)
                {
                    logService.LogError($"Failed to save year file {outputFilePath}: {ex.Message}");
                    await logService.LogExceptionAsync(ex, $"Error saving year file {outputFilePath}");

                    return;
                }

                // Thread-safe incrementing
                var processed = Interlocked.Increment(ref yearsProcessed);

                // Log only at intervals
                if (processed % logInterval == 0 || processed == totalYears)
                {
                    logService.Log($"Progress: {processed}/{totalYears} years processed.");
                }

                // Update progress
                if (processed % progressInterval == 0 || processed == totalYears)
                {
                    var progressPercentage = (double)processed / totalYears * 100;
                    progress.Report((int)progressPercentage);
                }
            });

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