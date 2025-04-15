using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;

namespace MAMEUtility;

public static class MameSoftwareList
{
    public static async void CreateAndSaveSoftwareList(string inputFolderPath, string outputFilePath, IProgress<int> progress, LogWindow logWindow)
    {
        try
        {
            if (!Directory.Exists(inputFolderPath))
            {
                logWindow.AppendLog("The specified folder does not exist.");
                throw new DirectoryNotFoundException("The specified folder does not exist.");
            }

            var files = Directory.GetFiles(inputFolderPath, "*.xml");
            if (files.Length == 0)
            {
                logWindow.AppendLog("No XML files found in the specified folder.");
                throw new FileNotFoundException("No XML files found in the specified folder.");
            }

            logWindow.AppendLog($"Found {files.Length} XML files to process.");

            // Define logging and progress intervals
            const int logInterval = 5; // Log every 5 files
            const int progressInterval = 2; // Update progress every 2 files

            // Use concurrent collection for thread safety
            var softwareList = new ConcurrentBag<XElement>();
            var processedCount = 0;

            // Use parallel processing with throttling
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };

            await Parallel.ForEachAsync(files, options, async (file, token) =>
            {
                try
                {
                    var doc = await Task.Run(() => XDocument.Load(file), token);

                    var softwares = doc.Descendants("software")
                        .Select(static software => new XElement("Software",
                            new XElement("SoftwareName", software.Attribute("name")?.Value),
                            new XElement("Description", software.Element("description")?.Value ?? "No Description")))
                        .ToList(); // Cache the result

                    // Add all softwares to the concurrent bag
                    foreach (var software in softwares)
                    {
                        softwareList.Add(software);
                    }

                    // Thread-safe incrementing
                    var processed = Interlocked.Increment(ref processedCount);

                    // Log only at intervals
                    if (processed % logInterval == 0 || processed == files.Length)
                    {
                        logWindow.AppendLog($"Progress: {processed}/{files.Length} files processed.");
                    }

                    // Update progress less frequently
                    if (processed % progressInterval == 0 || processed == files.Length)
                    {
                        var progressPercentage = (double)processed / files.Length * 100;
                        progress?.Report((int)progressPercentage);
                    }
                }
                catch (Exception ex)
                {
                    logWindow.AppendLog($"Skipping file '{file}' due to an error: {ex.Message}");
                    await LogError.LogAsync(ex, $"Skipping file '{file}' due to an error: {ex.Message}");
                }
            });

            logWindow.AppendLog("All files processed, saving consolidated XML file...");

            // Convert to list and save
            var outputDoc = new XDocument(new XElement("Softwares", softwareList.ToList()));
            await Task.Run(() => outputDoc.Save(outputFilePath));

            logWindow.AppendLog($"Consolidated XML file saved with {softwareList.Count} software entries to: {outputFilePath}");
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, "Error in method CreateAndSaveSoftwareList");
        }
    }
}