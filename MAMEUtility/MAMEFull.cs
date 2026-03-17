using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameFull
{
    public static Task CreateAndSaveMameFullAsync(
        string inputFilePath,
        string outputFilePathMameFull,
        IProgress<int> progress,
        ILogService logService,
        CancellationToken cancellationToken = default)
    {
        logService.Log($"Output file for MAME Full: {outputFilePathMameFull}");

        return Task.Run(async () =>
        {
            var outputDirectory = Path.GetDirectoryName(outputFilePathMameFull) ?? Path.GetTempPath();
            var tempFilePath = Path.Combine(outputDirectory, $"MameFull_{Guid.NewGuid()}.tmp");
            try
            {
                var readerSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    IgnoreWhitespace = true,
                    Async = true
                };

                var writerSettings = new XmlWriterSettings
                {
                    Indent = true,
                    Async = true
                };

                await using (var writerStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                await using (var writer = XmlWriter.Create(writerStream, writerSettings))
                await using (var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                {
                    var totalBytes = fileStream.Length;
                    var lastReportedProgress = -1;

                    using var reader = XmlReader.Create(fileStream, readerSettings);
                    await writer.WriteStartDocumentAsync();
                    await writer.WriteStartElementAsync(null, "Machines", null);

                    long processed = 0;
                    const int logInterval = 5000;

                    while (await reader.ReadAsync())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element && string.Equals(reader.Name, "machine", StringComparison.OrdinalIgnoreCase))
                        {
                            var name = reader.GetAttribute("name");
                            using var subReader = reader.ReadSubtree();
                            string? description = null;

                            while (await subReader.ReadAsync())
                            {
                                if (subReader.NodeType == XmlNodeType.Element && string.Equals(subReader.Name, "description", StringComparison.OrdinalIgnoreCase))
                                {
                                    description = await subReader.ReadElementContentAsStringAsync();
                                    break;
                                }
                            }

                            await writer.WriteStartElementAsync(null, "Machine", null);
                            await writer.WriteElementStringAsync(null, "MachineName", null, name ?? string.Empty);
                            await writer.WriteElementStringAsync(null, "Description", null, description ?? string.Empty);
                            await writer.WriteEndElementAsync();

                            processed++;
                            if (processed % logInterval == 0)
                                logService.Log($"Progress: {processed} machines processed...");
                        }

                        // Report progress based on stream position
                        if (totalBytes > 0)
                        {
                            var currentProgress = (int)((double)fileStream.Position / totalBytes * 99);
                            if (currentProgress > lastReportedProgress)
                            {
                                progress.Report(currentProgress);
                                lastReportedProgress = currentProgress;
                            }
                        }
                    }

                    await writer.WriteEndElementAsync();
                    await writer.WriteEndDocumentAsync();
                }

                // After closing the files, move the temporary file to the final destination
                if (File.Exists(outputFilePathMameFull))
                {
                    File.Delete(outputFilePathMameFull);
                }

                File.Move(tempFilePath, outputFilePathMameFull);

                progress.Report(100);
                logService.Log("MAME Full XML file created successfully.");
            }
            catch (OperationCanceledException)
            {
                // Clean up temp file on cancellation
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
                // Log and report the error
                await logService.LogExceptionAsync(ex, "Error in CreateAndSaveMameFullAsync");

                // Clean up temp file on failure
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                        /* Ignore cleanup errors */
                    }
                }

                throw;
            }
        }, cancellationToken);
    }
}
