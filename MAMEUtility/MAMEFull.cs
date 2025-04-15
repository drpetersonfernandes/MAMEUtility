using System.ComponentModel;
using System.Xml.Linq;

namespace MAMEUtility;

public static class MameFull
{
    public static Task CreateAndSaveMameFullAsync(XDocument inputDoc, string outputFilePathMameFull, BackgroundWorker worker, LogWindow logWindow)
    {
        logWindow.AppendLog($"Output file for MAME Full: {outputFilePathMameFull}");

        return Task.Run(() =>
        {
            // Count machines only once
            var machineElements = inputDoc.Descendants("machine").ToList();
            var totalMachines = machineElements.Count;
            var machinesProcessed = 0;

            logWindow.AppendLog($"Total machines: {totalMachines}");
            logWindow.AppendLog("Processing machines...");

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
                    logWindow.AppendLog($"Progress: {machinesProcessed}/{totalMachines} machines processed");
                }

                // Update progress bar less frequently
                if (machinesProcessed % progressInterval != 0 && machinesProcessed != totalMachines) continue;

                var progressPercentage = (int)((double)machinesProcessed / totalMachines * 100);
                worker.ReportProgress(progressPercentage);
            }

            logWindow.AppendLog("Saving to file...");
            allMachineDetailsDoc.Save(outputFilePathMameFull);
            worker.ReportProgress(100);
            logWindow.AppendLog("MAME Full XML file created successfully.");
        });
    }
}