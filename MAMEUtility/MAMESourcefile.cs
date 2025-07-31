using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameSourcefile
{
    public static async Task CreateAndSaveMameSourcefileAsync(XDocument inputDoc, string outputFolderMameSourcefile, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        logService.Log($"Output folder for MAME Sourcefile: {outputFolderMameSourcefile}");

        try
        {
            progress.Report(5);

            logService.Log("Extracting source files from XML...");
            var sourceFiles = await Task.Run(() =>
                inputDoc.Descendants("machine")
                    .Select(static m => (string?)m.Attribute("sourcefile"))
                    .Distinct()
                    .Where(static s => !string.IsNullOrEmpty(s))
                    .ToList(), cancellationToken);

            progress.Report(10);

            var totalSourceFiles = sourceFiles.Count;
            logService.Log($"Found {totalSourceFiles} unique source files to process.");

            // Thread-safe collections for parallel processing
            var sourcefileDocs = new ConcurrentDictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
            var generatedFileNames = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var processedCount = 0;

            var logInterval = Math.Max(1, totalSourceFiles / 10);

            // Parallel processing of source files
            logService.Log("Processing source files in parallel...");

            await Task.Run(() =>
            {
                Parallel.ForEach(sourceFiles, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                }, sourceFile =>
                {
                    if (string.IsNullOrWhiteSpace(sourceFile)) return;

                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        // Use the new FileNameHelper for robust sanitization of the actual filename
                        var safeSourceFileName = FileNameHelper.SanitizeForFileName(Path.GetFileNameWithoutExtension(sourceFile));

                        var uniqueSafeName = safeSourceFileName;
                        var counter = 1;
                        while (!generatedFileNames.TryAdd(uniqueSafeName, true))
                        {
                            uniqueSafeName = $"{safeSourceFileName}_{counter++}";
                        }

                        // Create the filtered document
                        XDocument filteredDoc = new(
                            new XElement("Machines",
                                from machine in inputDoc.Descendants("machine")
                                where (string?)machine.Attribute("sourcefile") == sourceFile
                                select new XElement("Machine",
                                    new XElement("MachineName", FileNameHelper.SanitizeForFileName(machine.Attribute("name")?.Value ?? "", "")), // Sanitize for name, not file
                                    new XElement("Description", FileNameHelper.SanitizeForXmlValue(machine.Element("description")?.Value ?? ""))
                                )
                            )
                        );

                        sourcefileDocs[uniqueSafeName] = filteredDoc;

                        var currentCount = Interlocked.Increment(ref processedCount);

                        if (currentCount % logInterval == 0 || currentCount == totalSourceFiles)
                        {
                            logService.Log($"Processing progress: {currentCount}/{totalSourceFiles} source files");
                            var progressPercentage = 10 + (int)((double)currentCount / totalSourceFiles * 70);
                            progress.Report(progressPercentage);
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.LogError($"Failed to process sourcefile '{sourceFile}': {ex.Message}");
                        _ = logService.LogExceptionAsync(ex, $"Error processing sourcefile '{sourceFile}'");
                    }
                });
            }, cancellationToken);

            progress.Report(80);

            // Save all collected documents to disk
            logService.Log("Saving sourcefile XML files...");
            var savedCount = 0;
            var totalToSave = sourcefileDocs.Count;

            foreach (var kvp in sourcefileDocs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var uniqueSafeName = kvp.Key;
                var filteredDoc = kvp.Value;
                var outputFilePath = Path.Combine(outputFolderMameSourcefile, $"{uniqueSafeName}.xml");

                try
                {
                    await Task.Run(() => filteredDoc.Save(outputFilePath), cancellationToken);
                }
                catch (Exception ex)
                {
                    logService.LogError($"Failed to save file for sourcefile '{uniqueSafeName}': {ex.Message}");
                    await logService.LogExceptionAsync(ex, $"Error saving sourcefile document for '{uniqueSafeName}'");
                }

                savedCount++;
                if (savedCount % 10 == 0 || savedCount == totalToSave)
                {
                    var saveProgress = 80 + (int)((double)savedCount / totalToSave * 20);
                    progress.Report(saveProgress);
                }
            }

            progress.Report(100);
            logService.Log("All source file documents created successfully.");
        }
        catch (Exception ex)
        {
            logService.LogError("An error occurred: " + ex.Message);
            await logService.LogExceptionAsync(ex, "Error in method MAMESourcefile.CreateAndSaveMameSourcefileAsync");
            throw;
        }
    }
}
