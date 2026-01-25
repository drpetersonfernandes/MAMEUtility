using System.IO;
using System.Xml;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameManufacturer
{
    public static Task CreateAndSaveMameManufacturerAsync(string inputFilePath, string outputFolderMameManufacturer, IProgress<int> progress, ILogService logService, CancellationToken cancellationToken = default)
    {
        logService.Log($"Streaming manufacturers from: {inputFilePath}");

        return Task.Run(() =>
        {
            var writers = new Dictionary<string, XmlWriter>(StringComparer.OrdinalIgnoreCase);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };

            try
            {
                using var reader = XmlReader.Create(inputFilePath, settings);
                long processed = 0;

                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader is { NodeType: XmlNodeType.Element, Name: "machine" })
                    {
                        var name = reader.GetAttribute("name") ?? "";
                        var emulation = reader.GetAttribute("emulation") ?? "";

                        // Skip BIOS and bad emulation early
                        if (name.Contains("bios", StringComparison.OrdinalIgnoreCase) || emulation == "preliminary")
                        {
                            reader.Skip();
                            continue;
                        }

                        using var subReader = reader.ReadSubtree();
                        string? manufacturer = null;
                        string? description = null;

                        while (subReader.Read())
                        {
                            if (subReader.NodeType == XmlNodeType.Element)
                            {
                                switch (subReader.Name)
                                {
                                    case "manufacturer":
                                        manufacturer = subReader.ReadElementContentAsString();
                                        break;
                                    case "description":
                                        description = subReader.ReadElementContentAsString();
                                        break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(manufacturer) && description != null && !description.Contains("bios", StringComparison.OrdinalIgnoreCase))
                        {
                            var safeName = FileNameHelper.SanitizeForFileName(manufacturer);
                            if (!writers.TryGetValue(safeName, out var writer))
                            {
                                var path = Path.Combine(outputFolderMameManufacturer, safeName + ".xml");
                                writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });
                                writer.WriteStartElement("Machines");
                                writers.Add(safeName, writer);
                            }

                            writer.WriteStartElement("Machine");
                            writer.WriteElementString("MachineName", name);
                            writer.WriteElementString("Description", description);
                            writer.WriteEndElement();
                        }

                        processed++;
                        if (processed % 10000 == 0) logService.Log($"Processed {processed} machines...");
                    }
                }
            }
            finally
            {
                foreach (var w in writers.Values)
                {
                    w.WriteEndElement();
                    w.Dispose();
                }
            }

            logService.Log("Manufacturer lists generated successfully.");
            progress.Report(100);
        }, cancellationToken);
    }
}
