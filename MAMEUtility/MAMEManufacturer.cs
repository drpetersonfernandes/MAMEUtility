using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MAMEUtility
{
    public partial class MAMEManufacturer
    {
        public static async Task CreateAndSaveMAMEManufacturerAsync(XDocument inputDoc, string outputFolderMAMEManufacturer, IProgress<int> progress)
        {
            Console.WriteLine($"Output folder for MAME Manufacturer: {outputFolderMAMEManufacturer}");

            try
            {
                // Extract unique manufacturers
                var manufacturers = inputDoc.Descendants("machine")
                    .Select(m => (string?)m.Element("manufacturer"))
                    .Distinct()
                    .Where(m => !string.IsNullOrEmpty(m));

                int totalManufacturers = manufacturers.Count();
                int manufacturersProcessed = 0;

                // Iterate over each manufacturer and create an XML for each
                foreach (var manufacturer in manufacturers)
                {
                    if (manufacturer != null)
                    {
                        string safeManufacturerName = RemoveExtraWhitespace(manufacturer
                            .Replace("<", "")
                            .Replace(">", "")
                            .Replace(":", "")
                            .Replace("\"", "")
                            .Replace("/", "")
                            .Replace("\\", "")
                            .Replace("|", "")
                            .Replace("?", "")
                            .Replace("*", "")
                            .Replace("unknown", "UnknownManufacturer")
                            .Trim())
                            .Replace("&amp;", "&");  // Replace &amp; with & in the filename.

                        if (safeManufacturerName.Contains("bootleg", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine($"Skipping {manufacturer} as it contains 'bootleg'.");
                            continue;
                        }

                        string outputFilePath = System.IO.Path.Combine(outputFolderMAMEManufacturer, $"{safeManufacturerName}.xml");
                        Console.WriteLine($"Attempting to create file for: {safeManufacturerName}.xml");

                        await CreateAndSaveFilteredDocumentAsync(inputDoc, outputFilePath, manufacturer, safeManufacturerName);

                        manufacturersProcessed++;
                        double progressPercentage = (double)manufacturersProcessed / totalManufacturers * 100;
                        progress.Report((int)progressPercentage);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private static async Task CreateAndSaveFilteredDocumentAsync(XDocument inputDoc, string outputPath, string manufacturer, string safeManufacturerName)
        {
            XDocument filteredDoc = CreateFilteredDocument(inputDoc, manufacturer);

            try
            {
                await Task.Run(() => filteredDoc.Save(outputPath));
                Console.WriteLine($"Successfully created: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create file for {safeManufacturerName}. Error: {ex.Message}");
            }
        }

        private static XDocument CreateFilteredDocument(XDocument inputDoc, string manufacturer)
        {
            bool predicate(XElement machine) =>
                (machine.Element("manufacturer")?.Value ?? "") == manufacturer &&
                !(machine.Attribute("name")?.Value.Contains("bootleg", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                !(machine.Element("description")?.Value.Contains("bootleg", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                !(machine.Element("description")?.Value.Contains("prototype", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                !(machine.Element("description")?.Value.Contains("playchoice", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                !(machine.Attribute("name")?.Value.Contains("bios", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                !(machine.Element("description")?.Value.Contains("bios", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                (machine.Element("driver")?.Attribute("emulation")?.Value ?? "") == "good" &&
                machine.Attribute("cloneof") == null;

            // Retrieve the matched machines
            var matchedMachines = inputDoc.Descendants("machine").Where(predicate).ToList();

            // Create a new XML document for machines based on the matched machines
            XDocument filteredDoc = new(
                new XElement("Machines",
                    from machine in matchedMachines
                    select new XElement("Machine",
                        new XElement("MachineName", RemoveExtraWhitespace(machine.Attribute("name")?.Value ?? "").Replace("&amp;", "&")),
                        new XElement("Description", RemoveExtraWhitespace(machine.Element("description")?.Value ?? "").Replace("&amp;", "&"))
                    // Apply RemoveExtraWhitespace to any other elements as needed
                    )
                )
            );

            return filteredDoc;
        }

        private static string RemoveExtraWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return MyRegex().Replace(input, " ");
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}