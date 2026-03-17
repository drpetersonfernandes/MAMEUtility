using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameSourcefile
{
    public static async Task CreateAndSaveMameSourcefileAsync(string inputFilePath, string outputFolderMameSourcefile, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        logService.Log($"Processing source files from: {inputFilePath}");
        logService.Log($"Output folder: {outputFolderMameSourcefile}");

        try
        {
            var sourcefileData = new Dictionary<string, List<(string Name, string Description)>>(StringComparer.OrdinalIgnoreCase);
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, Async = true };

            // Single pass: collect all required data grouped by source file
            await using (var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                var totalBytes = fileStream.Length;
                var lastReportedProgress = -1;
                long processedCount = 0;

                using var reader = XmlReader.Create(fileStream, readerSettings);
                while (await reader.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element && string.Equals(reader.Name, "machine", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = reader.GetAttribute("name") ?? "";
                        var sourceFile = reader.GetAttribute("sourcefile") ?? "";
                        string? description = null;

                        using (var subReader = reader.ReadSubtree())
                        {
                            while (await subReader.ReadAsync())
                            {
                                if (subReader.NodeType == XmlNodeType.Element && string.Equals(subReader.Name, "description", StringComparison.OrdinalIgnoreCase))
                                {
                                    description = await subReader.ReadElementContentAsStringAsync();
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(sourceFile))
                        {
                            if (!sourcefileData.TryGetValue(sourceFile, out var machines))
                            {
                                machines = new List<(string Name, string Description)>();
                                sourcefileData[sourceFile] = machines;
                            }

                            machines.Add((name, description ?? string.Empty));
                        }

                        processedCount++;
                        if (processedCount % 10000 == 0)
                        {
                            logService.Log($"Scanned {processedCount} machines...");
                        }
                    }

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

            logService.Log($"Found {sourcefileData.Count} unique source files. Saving files...");

            // Second phase: Write the collected data to files
            var totalSourceFiles = sourcefileData.Count;
            var sourceFilesSavedCount = 0;
            var generatedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var writerSettings = new XmlWriterSettings { Indent = true, Async = true };

            foreach (var kvp in sourcefileData)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourceFile = kvp.Key;
                var machines = kvp.Value;

                var baseSafeName = FileNameHelper.SanitizeForFileName(Path.GetFileNameWithoutExtension(sourceFile));
                var uniqueSafeName = baseSafeName;
                var counter = 1;
                while (generatedFileNames.Contains(uniqueSafeName))
                {
                    uniqueSafeName = $"{baseSafeName}_{counter++}";
                }

                generatedFileNames.Add(uniqueSafeName);

                var outputFilePath = Path.Combine(outputFolderMameSourcefile, $"{uniqueSafeName}.xml");

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

                sourceFilesSavedCount++;
                var currentProgress = 50 + (int)((double)sourceFilesSavedCount / totalSourceFiles * 50);
                progress.Report(currentProgress);

                if (sourceFilesSavedCount % 10 == 0 || sourceFilesSavedCount == totalSourceFiles)
                {
                    logService.Log($"Saved {sourceFilesSavedCount}/{totalSourceFiles} source file XMLs.");
                }
            }

            progress.Report(100);
            logService.Log("All source file documents created successfully.");
        }
        catch (Exception ex)
        {
            await logService.LogExceptionAsync(ex, "Error in method MAMESourcefile.CreateAndSaveMameSourcefileAsync");
            throw;
        }
    }
}
