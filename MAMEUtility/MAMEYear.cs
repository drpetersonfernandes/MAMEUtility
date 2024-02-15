using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MAMEUtility
{
    public partial class MAMEYear
    {
        public static async Task CreateAndSaveMAMEYear(XDocument inputDoc, string outputFolderMAMEYear, IProgress<int> progress)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"Output folder for MAME Year: {outputFolderMAMEYear}");

                try
                {
                    // Extract unique years
                    var years = inputDoc.Descendants("machine")
                        .Select(m => (string?)m.Element("year"))
                        .Distinct()
                        .Where(y => !string.IsNullOrEmpty(y));

                    int totalYears = years.Count();
                    int yearsProcessed = 0;

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
                                    )
                                )
                            );

                            // Save the XML document for the year
                            string outputFilePath = Path.Combine(outputFolderMAMEYear, $"{year.Replace("?", "X")}.xml");
                            yearDoc.Save(outputFilePath);
                            Console.WriteLine($"Successfully created XML file for year {year}: {outputFilePath}");

                            yearsProcessed++;
                            double progressPercentage = (double)yearsProcessed / totalYears * 100;
                            progress.Report((int)progressPercentage);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            });
        }
    }
}