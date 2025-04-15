using System.IO;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

public static class MameSourcefile
{
    public static async Task CreateAndSaveMameSourcefileAsync(XDocument inputDoc, string outputFolderMameSourcefile, IProgress<int> progress, ILogService logService)
    {
        logService.Log($"Output folder for MAME Sourcefile: {outputFolderMameSourcefile}");

        try
        {
            // Extract unique source files and cache the result
            var sourceFiles = inputDoc.Descendants("machine")
                .Select(static m => (string?)m.Attribute("sourcefile"))
                .Distinct()
                .Where(static s => !string.IsNullOrEmpty(s))
                .ToList();

            var totalSourceFiles = sourceFiles.Count;
            var sourceFilesProcessed = 0;

            // Define logging and progress intervals
            const int logInterval = 10; // Log every 10 sourcefiles
            const int progressInterval = 5; // Update progress every 5 sourcefiles

            logService.Log($"Found {totalSourceFiles} unique source files to process.");

            // Use parallel processing with throttling
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };

            await Parallel.ForEachAsync(sourceFiles, options, async (sourceFile, token) =>
            {
                if (string.IsNullOrWhiteSpace(sourceFile)) return;

                // Remove the ".cpp" extension and clean filename in one pass
                var safeSourceFileName = ReplaceInvalidFileNameChars(Path.GetFileNameWithoutExtension(sourceFile));
                var outputFilePath = Path.Combine(outputFolderMameSourcefile, $"{safeSourceFileName}.xml");

                // Filter and create document
                XDocument filteredDoc = new(
                    new XElement("Machines",
                        from machine in inputDoc.Descendants("machine")
                        where (string?)machine.Attribute("sourcefile") == sourceFile
                        select new XElement("Machine",
                            new XElement("MachineName", machine.Attribute("name")?.Value),
                            new XElement("Description", machine.Element("description")?.Value)
                        )
                    )
                );

                // Save document
                try
                {
                    await Task.Run(() => filteredDoc.Save(outputFilePath), token);
                }
                catch (Exception ex)
                {
                    logService.LogError($"Failed to create file for {sourceFile}. Error: {ex.Message}");
                    await logService.LogExceptionAsync(ex, $"Error saving sourcefile document for {sourceFile}");

                    return;
                }


                // Thread-safe incrementing
                var processed = Interlocked.Increment(ref sourceFilesProcessed);

                // Log only at intervals
                if (processed % logInterval == 0 || processed == totalSourceFiles)
                {
                    logService.Log($"Progress: {processed}/{totalSourceFiles} source files processed.");
                }

                // Update progress
                if (processed % progressInterval == 0 || processed == totalSourceFiles)
                {
                    var progressPercentage = (double)processed / totalSourceFiles * 100;
                    progress.Report((int)progressPercentage);
                }
            });

            logService.Log("All source file documents created successfully.");
        }
        catch (Exception ex)
        {
            logService.LogError("An error occurred: " + ex.Message);
            await logService.LogExceptionAsync(ex, "Error in method MAMESourcefile.CreateAndSaveMameSourcefileAsync");
        }
    }

    private static string ReplaceInvalidFileNameChars(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }
}