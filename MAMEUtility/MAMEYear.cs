using System.IO;
using System.Xml.Linq;

namespace MAMEUtility;

public static class MameYear
{
    public static Task CreateAndSaveMameYear(XDocument inputDoc, string outputFolderMameYear, IProgress<int> progress, LogWindow logWindow)
    {
        logWindow.AppendLog($"Output folder for MAME Year: {outputFolderMameYear}");

        return Task.Run(async () =>
        {
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

                logWindow.AppendLog($"Found {totalYears} unique years to process.");

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
                    await Task.Run(() => yearDoc.Save(outputFilePath), token);

                    // Thread-safe incrementing
                    var processed = Interlocked.Increment(ref yearsProcessed);

                    // Log only at intervals
                    if (processed % logInterval == 0 || processed == totalYears)
                    {
                        logWindow.AppendLog($"Progress: {processed}/{totalYears} years processed.");
                    }

                    // Update progress less frequently
                    if (processed % progressInterval == 0 || processed == totalYears)
                    {
                        var progressPercentage = (double)processed / totalYears * 100;
                        progress.Report((int)progressPercentage);
                    }
                });

                logWindow.AppendLog("All year files created successfully.");
            }
            catch (Exception ex)
            {
                logWindow.AppendLog("An error occurred: " + ex.Message);
                await LogError.LogAsync(ex, "Error in method MAMEYear.CreateAndSaveMameYear");
            }
        });
    }
}