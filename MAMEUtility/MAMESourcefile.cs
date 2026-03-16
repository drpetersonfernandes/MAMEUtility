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
            // First pass: collect unique source files
            var sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, Async = true };

            await using (var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                var totalBytes = fileStream.Length;
                var lastReportedProgress = -1;
                long processedCount = 0;

                using var reader = XmlReader.Create(fileStream, readerSettings);
                while (await reader.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader is { NodeType: XmlNodeType.Element, Name: "machine" })
                    {
                        var sourceFile = reader.GetAttribute("sourcefile") ?? "";

                        if (!string.IsNullOrWhiteSpace(sourceFile))
                        {
                            sourceFiles.Add(sourceFile);
                        }

                        processedCount++;
                        if (processedCount % 10000 == 0)
                        {
                            logService.Log($"Scanned {processedCount} machines for source files...");
                        }
                    }

                    if (totalBytes > 0)
                    {
                        var currentProgress = (int)((double)fileStream.Position / totalBytes * 25);
                        if (currentProgress > lastReportedProgress)
                        {
                            progress.Report(currentProgress);
                            lastReportedProgress = currentProgress;
                        }
                    }
                }
            }

            progress.Report(25);
            logService.Log($"Found {sourceFiles.Count} unique source files. Processing each...");

            // Second pass: for each source file, stream through file and write matches
            var totalSourceFiles = sourceFiles.Count;
            var sourceFilesSavedCount = 0;
            var generatedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var writerSettings = new XmlWriterSettings { Indent = true, Async = true };

            foreach (var sourceFile in sourceFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                    // Stream through input file and write matching machines
                    await using var readStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                    using var readReader = XmlReader.Create(readStream, readerSettings);

                    while (await readReader.ReadAsync())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (readReader is { NodeType: XmlNodeType.Element, Name: "machine" })
                        {
                            var name = readReader.GetAttribute("name") ?? "";
                            var machineSourceFile = readReader.GetAttribute("sourcefile") ?? "";
                            string? description = null;

                            using (var subReader = readReader.ReadSubtree())
                            {
                                while (await subReader.ReadAsync())
                                {
                                    if (subReader is { NodeType: XmlNodeType.Element, Name: "description" })
                                    {
                                        description = await subReader.ReadElementContentAsStringAsync();
                                        break;
                                    }
                                }
                            }

                            if (string.Equals(machineSourceFile, sourceFile, StringComparison.OrdinalIgnoreCase))
                            {
                                await writer.WriteStartElementAsync(null, "Machine", null);
                                await writer.WriteElementStringAsync(null, "MachineName", null, name);
                                await writer.WriteElementStringAsync(null, "Description", null, description ?? string.Empty);
                                await writer.WriteEndElementAsync();
                            }
                        }
                    }

                    await writer.WriteEndElementAsync();
                    await writer.WriteEndDocumentAsync();
                }

                sourceFilesSavedCount++;
                var currentProgress = 25 + (int)((double)sourceFilesSavedCount / totalSourceFiles * 75);
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
