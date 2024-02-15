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

                    // Calculate progress percentage
                    progressPercentage = (int)((double)machinesProcessed / totalMachines * 100);

                    // Report progress
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

                // Save the document asynchronously
                allMachineDetailsDoc.Save(outputFilePathMAMEFull);

                Console.WriteLine("MAMEFull saved.");

                // Report completion
                worker.ReportProgress(100);
            });
        }
    }
}
