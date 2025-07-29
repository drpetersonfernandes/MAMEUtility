using System.ComponentModel;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility;

public static class MameFull
{
    public static Task CreateAndSaveMameFullAsync(XDocument inputDoc, string outputFilePathMameFull, BackgroundWorker worker, ILogService logService)
    {
        logService.Log($"Output file for MAME Full: {outputFilePathMameFull}");

        return Task.Run(() =>
        {
            var machineElements = inputDoc.Descendants("machine").ToList();
            var totalMachines = machineElements.Count;
            var machinesProcessed = 0;

            logService.Log($"Total machines: {totalMachines}");
            logService.Log("Processing machines...");

            const int logInterval = 500;
            const int progressInterval = 50;

            XDocument allMachineDetailsDoc = new(new XElement("Machines"));

            foreach (var machine in machineElements)
            {
                var machineName = machine.Attribute("name")?.Value;
                var description = machine.Element("description")?.Value;

                allMachineDetailsDoc.Root?.Add(
                    new XElement("Machine",
                        new XElement("MachineName", machineName),
                        new XElement("Description", description)
                    )
                );

                machinesProcessed++;

                if (machinesProcessed % logInterval == 0 || machinesProcessed == totalMachines)
                {
                    logService.Log($"Progress: {machinesProcessed}/{totalMachines} machines processed");
                }

                if (machinesProcessed % progressInterval != 0 && machinesProcessed != totalMachines) continue;

                var progressPercentage = (int)((double)machinesProcessed / totalMachines * 100);
                worker.ReportProgress(progressPercentage);
            }

            logService.Log("Saving to file...");
            allMachineDetailsDoc.Save(outputFilePathMameFull);
            worker.ReportProgress(100);
            logService.Log("MAME Full XML file created successfully.");
        });
    }
}
