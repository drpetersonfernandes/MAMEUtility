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
            // Count machines only once
            var machineElements = inputDoc.Descendants("machine").ToList();
            var totalMachines = machineElements.Count;
            var machinesProcessed = 0;

            logService.Log($"Total machines: {totalMachines}");
            logService.Log("Processing machines...");

            // Log progress at regular intervals instead of for every machine
            const int logInterval = 500; // Log every 500 machines
            const int progressInterval = 50; // Update progress every 50 machines

            // Create the result document in a single pass
            XDocument allMachineDetailsDoc = new(
                new XElement("Machines")
            );

            foreach (var machine in machineElements)
            {
                var machineName = machine.Attribute("name")?.Value;
                var description = machine.Element("description")?.Value;

                // Add to the result document
                allMachineDetailsDoc.Root?.Add(
                    new XElement("Machine",
                        new XElement("MachineName", machineName),
                        new XElement("Description", description)
                    )
                );

                machinesProcessed++;

                // Log only occasionally to avoid UI bottlenecks
                if (machinesProcessed % logInterval == 0 || machinesProcessed == totalMachines)
                {
                    logService.Log($"Progress: {machinesProcessed}/{totalMachines} machines processed");
                }

                // Update progress bar
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