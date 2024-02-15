using System.IO;
using System.Xml.Linq;

namespace MAMEUtility
{
    public partial class MAMEYear
    {
        public static void CreateAndSaveMAMEYear(XDocument inputDoc, string outputFolderMAMEYear)
        {
            Console.WriteLine($"Output folder for MAME Year: {outputFolderMAMEYear}");

            try
            {
                // Extract unique years
                var years = inputDoc.Descendants("machine")
                    .Select(m => (string?)m.Element("year"))
                    .Distinct()
                    .Where(y => !string.IsNullOrEmpty(y));

                // Iterate over each unique year
                foreach (var year in years)
                {
                    if (year != null)
                    {
                        // Filter machines based on year
                        var machinesForYear = inputDoc.Descendants("machine")
                            .Where(m => (string?)m.Element("year") == year);

                        // Create XML document for the year
                        XDocument yearDoc = new(
                            new XElement("Machines",
                                from machine in machinesForYear
                                select new XElement("Machine",
                                    new XElement("MachineName", machine.Attribute("name")?.Value ?? ""),
                                    new XElement("Description", machine.Element("description")?.Value ?? "")
                                // Add other elements as needed
                                )
                            )
                        );

                        // Save the XML document for the year
                        string outputFilePath = Path.Combine(outputFolderMAMEYear, $"{year.Replace("?", "X")}.xml");
                        yearDoc.Save(outputFilePath);
                        Console.WriteLine($"Successfully created XML file for year {year}: {outputFilePath}");
                    }
                }

                Console.WriteLine("XML files created successfully for all years.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

    }
}
