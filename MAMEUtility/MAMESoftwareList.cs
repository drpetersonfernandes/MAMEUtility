using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameSoftwareList
{
    public static async Task CreateAndSaveSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(inputFolderPath))
            {
                logService.LogError($"The specified folder does not exist: {inputFolderPath}. Operation cancelled.");
                progress.Report(0); // Ensure progress is reset or stays at 0
                return; // Gracefully exit instead of throwing
            }

            progress.Report(5);

            var files = Directory.GetFiles(inputFolderPath, "*.xml");
            if (files.Length == 0)
            {
                logService.LogWarning($"No XML files found in the specified folder: {inputFolderPath}. Operation cancelled.");
                progress.Report(100); // Consider it "completed" as nothing to process
                return; // Gracefully exit instead of throwing
            }

            logService.Log($"Found {files.Length} XML files to process.");

            // Thread-safe collection for parallel processing
            var allSoftware = new ConcurrentBag<XElement>();
            var processedCount = 0;
            var totalFiles = files.Length;

            var logInterval = Math.Max(1, totalFiles / 10);

            // Parallel processing of XML files
            logService.Log("Processing XML files in parallel...");

            await Task.Run(() =>
            {
                Parallel.ForEach(files, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                }, file =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var doc = XDocument.Load(file);

                        var softwares = doc.Descendants("software")
                            .Select(static software => new XElement("Software",
                                new XElement("SoftwareName", software.Attribute("name")?.Value),
                                new XElement("Description", software.Element("description")?.Value ?? "No Description")))
                            .ToList();

                        foreach (var software in softwares)
                        {
                            allSoftware.Add(software);
                        }

                        var currentCount = Interlocked.Increment(ref processedCount);

                        if (currentCount % logInterval == 0 || currentCount == totalFiles) // Changed to == for final log
                        {
                            logService.Log($"Processing progress: {currentCount}/{totalFiles} files processed");
                            var progressPercentage = 10 + (int)((double)currentCount / totalFiles * 80);
                            progress.Report(progressPercentage);
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.LogWarning($"Skipping file '{file}' due to an error: {ex.Message}");
                        _ = logService.LogExceptionAsync(ex, $"Skipping file '{file}' due to an error");
                    }
                });
            }, cancellationToken);

            progress.Report(90);

            // Create and save the consolidated XML
            logService.Log("Creating consolidated XML file...");
            var softwareList = allSoftware.ToList();
            var outputDoc = new XDocument(new XElement("Softwares", softwareList));

            await Task.Run(() => outputDoc.Save(outputFilePath), cancellationToken);

            progress.Report(100);
            logService.Log($"Consolidated XML file saved with {softwareList.Count} software entries to: {outputFilePath}");
        }
        catch (OperationCanceledException)
        {
            // Re-throw OperationCanceledException to be handled by the caller (MainWindow)
            throw;
        }
        catch (Exception ex)
        {
            // Only log and re-throw for unexpected exceptions
            await logService.LogExceptionAsync(ex, "Error in method CreateAndSaveSoftwareListAsync");
            throw;
        }
    }
}