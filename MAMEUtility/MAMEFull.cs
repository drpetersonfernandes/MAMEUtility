using System.Xml.Linq;
using MAMEUtility.Interfaces;

namespace MAMEUtility;

public static class MameFull
{
    public static Task CreateAndSaveMameFullAsync(
        XDocument inputDoc,
        string outputFilePathMameFull,
        IProgress<int> progress,
        ILogService logService,
        CancellationToken cancellationToken = default)
    {
        logService.Log($"Output file for MAME Full: {outputFilePathMameFull}");

        return Task.Run(() =>
        {
            var machineElements = inputDoc.Descendants("machine").ToList();
            var totalMachines = machineElements.Count;
            var processed = 0;

            logService.Log($"Total machines: {totalMachines}");
            logService.Log("Processing machines...");

            const int logInterval = 500;
            const int progressInterval = 50;

            var doc = new XDocument(new XElement("Machines"));

            foreach (var m in machineElements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (doc.Root != null)
                {
                    doc.Root.Add(
                        new XElement("Machine",
                            new XElement("MachineName", m.Attribute("name")?.Value),
                            new XElement("Description", m.Element("description")?.Value)));
                }

                processed++;

                if (processed % logInterval == 0 || processed == totalMachines)
                    logService.Log($"Progress: {processed}/{totalMachines} machines processed");

                if (processed % progressInterval == 0 || processed == totalMachines)
                    progress.Report((int)((double)processed / totalMachines * 100));
            }

            logService.Log("Saving to file...");
            doc.Save(outputFilePathMameFull);
            progress.Report(100);
            logService.Log("MAME Full XML file created successfully.");
        }, cancellationToken);
    }
}