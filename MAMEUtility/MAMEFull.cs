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

        return Task.Run(() =>
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreWhitespace = true
            };

            using var reader = XmlReader.Create(inputFilePath, settings);
            using var writer = XmlWriter.Create(outputFilePathMameFull, new XmlWriterSettings { Indent = true });

            writer.WriteStartDocument();
            writer.WriteStartElement("Machines");

            long processed = 0;
            const int logInterval = 5000;

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader is { NodeType: XmlNodeType.Element, Name: "machine" })
                {
                    var name = reader.GetAttribute("name");
                    using var subReader = reader.ReadSubtree();
                    string? description = null;

                    while (subReader.Read())
                    {
                        if (subReader is { NodeType: XmlNodeType.Element, Name: "description" })
                        {
                            description = subReader.ReadElementContentAsString();
                            break;
                        }
                    }

                    writer.WriteStartElement("Machine");
                    writer.WriteElementString("MachineName", name);
                    writer.WriteElementString("Description", description ?? "");
                    writer.WriteEndElement();

                    processed++;
                    if (processed % logInterval == 0)
                        logService.Log($"Progress: {processed} machines processed...");
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();

            progress.Report(100);
            logService.Log("MAME Full XML file created successfully.");
        }, cancellationToken);
    }
}