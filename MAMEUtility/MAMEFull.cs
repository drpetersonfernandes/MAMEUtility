using System.Xml.Linq;

namespace MAMEUtility
{
    public class MAMEFull
    {
        public static void CreateAndSaveMAMEFull(XDocument inputDoc, string outputFilePathMAMEFull)
        {
            int totalMachines = inputDoc.Descendants("machine").Count();
            int machinesProcessed = 0;

            Console.WriteLine($"Total machines: {totalMachines}");

            foreach (var machine in inputDoc.Descendants("machine"))
            {
                string? machineName = machine.Attribute("name")?.Value;
                string? description = machine.Element("description")?.Value;

                Console.WriteLine($"Processing machine: {machineName}");

                machinesProcessed++;
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

            Console.WriteLine("MAMEFull saved.");
        }
    }
}
