using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameSoftwareList
{
    public static async Task CreateAndSaveSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        var outputDirectory = Path.GetDirectoryName(outputFilePath) ?? Path.GetTempPath();
        var tempFilePath = Path.Combine(outputDirectory, $"MameSoftwareList_{Guid.NewGuid()}.tmp");

        try
        {
            if (!Directory.Exists(inputFolderPath))
            {
                logService.LogError($"The specified folder does not exist: {inputFolderPath}. Operation cancelled.");
                progress.Report(0);
                return;
            }

            progress.Report(5);

            var outputFullPath = Path.GetFullPath(outputFilePath);
            var files = Directory.GetFiles(inputFolderPath, "*.xml")
                .Where(f => !string.Equals(Path.GetFullPath(f), outputFullPath, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (files.Length == 0)
            {
                logService.LogWarning($"No XML files found in the specified folder: {inputFolderPath}. Operation cancelled.");
                progress.Report(100);
                return;
            }

            logService.Log($"Found {files.Length} XML files to process. Streaming output...");

            var processedCount = 0;
            var totalFiles = files.Length;
            var logInterval = Math.Max(1, totalFiles / 10);
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, Async = true };
            var writerSettings = new XmlWriterSettings { Async = true, Indent = true };

            await using (var writerStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            await using (var xmlWriter = XmlWriter.Create(writerStream, writerSettings))
            {
                await xmlWriter.WriteStartDocumentAsync();
                await xmlWriter.WriteStartElementAsync(null, "Softwares", null);

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                        using var reader = XmlReader.Create(fileStream, readerSettings);

                        while (await reader.ReadAsync())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType == XmlNodeType.Element && string.Equals(reader.Name, "software", StringComparison.OrdinalIgnoreCase))
                            {
                                var name = reader.GetAttribute("name");
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

                                await xmlWriter.WriteStartElementAsync(null, "Software", null);
                                await xmlWriter.WriteElementStringAsync(null, "SoftwareName", null, name ?? string.Empty);
                                await xmlWriter.WriteElementStringAsync(null, "Description", null, description ?? "No Description");
                                await xmlWriter.WriteEndElementAsync();
                            }
                        }

                        processedCount++;
                        if (processedCount % logInterval == 0 || processedCount == totalFiles)
                        {
                            logService.Log($"Processing progress: {processedCount}/{totalFiles} files processed");
                            var progressPercentage = 5 + (int)((double)processedCount / totalFiles * 95);
                            progress.Report(progressPercentage);
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.LogWarning($"Skipping file '{Path.GetFileName(file)}' due to an error: {ex.Message}");
                        await logService.LogExceptionAsync(ex, $"Skipping file '{file}' due to an error");
                    }
                }

                await xmlWriter.WriteEndElementAsync();
                await xmlWriter.WriteEndDocumentAsync();
            }

            // After closing the files, move the temporary file to the final destination
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            File.Move(tempFilePath, outputFilePath);

            progress.Report(100);
            logService.Log($"Consolidated XML file saved to: {outputFilePath}");
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    /* Ignore */
                }
            }

            throw;
        }
        catch (Exception ex)
        {
            await logService.LogExceptionAsync(ex, "Error in method CreateAndSaveSoftwareListAsync");
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    /* Ignore */
                }
            }

            throw;
        }
    }
}
