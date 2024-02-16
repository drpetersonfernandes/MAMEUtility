using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MAMEUtility
{
    public class MAMEFull
    {
        public static async Task CreateAndSaveMAMEFullAsync(XDocument inputDoc, string outputFilePathMAMEFull, BackgroundWorker worker)
        {
            Console.WriteLine($"Output folder for MAME Full: {outputFilePathMAMEFull}");

            await Task.Run(() =>
            {
                int totalMachines = inputDoc.Descendants("machine").Count();
                int machinesProcessed = 0;
                int progressPercentage = 0;

                Console.WriteLine($"Total machines: {totalMachines}");

                foreach (var machine in inputDoc.Descendants("machine"))
                {
                    string? machineName = machine.Attribute("name")?.Value;
                    string? description = machine.Element("description")?.Value;

                    Console.WriteLine($"Processing machine: {machineName}");

                    machinesProcessed++;
                    progressPercentage = (int)((double)machinesProcessed / totalMachines * 100);
                    worker.ReportProgress(progressPercentage);

                    Console.WriteLine($"Progress: {machinesProcessed}/{totalMachines}");
                }

                Console.WriteLine("Saving to file...");

                XDocument allMachineDetailsDoc = new(
                    new XElement("Machines",
                        from machine in inputDoc.Descendants("machine")
                        select new XElement("Machine",
                            new XElement("MachineName", machine.Attribute("name")?.Value),
                            new XElement("Description", machine.Element("description")?.Value)
                        )
                    )
                );

                allMachineDetailsDoc.Save(outputFilePathMAMEFull);
                worker.ReportProgress(100);
            });
        }
    }
}