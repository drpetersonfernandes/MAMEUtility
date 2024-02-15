using System.Xml.Linq;

namespace MAMEUtility
{
    public partial class MAMEManufacturer
    {
        public static void CreateAndSaveMAMEManufacturer(XDocument inputDoc, string outputFolderMAMEManufacturer)
        {
            Console.WriteLine($"Output folder for MAME Manufacturer: {outputFolderMAMEManufacturer}");

            try
            {
                // Extract unique manufacturers
                var manufacturers = inputDoc.Descendants("machine")
                    .Select(m => (string?)m.Element("manufacturer"))
                    .Distinct()
                    .Where(m => !string.IsNullOrEmpty(m));

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
                        CreateAndSaveFilteredDocument(inputDoc, outputFilePath, manufacturer, safeManufacturerName);
                    }
                }
                Console.WriteLine("Data extracted and saved successfully for all manufacturers.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private static void CreateAndSaveFilteredDocument(XDocument inputDoc, string outputPath, string manufacturer, string safeManufacturerName)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            bool predicate(XElement machine) =>
                (string)(machine.Element("manufacturer")?.Value ?? "") == manufacturer &&
                !(machine.Attribute("name")?.Value.Contains("bootleg", StringComparison.InvariantCultureIgnoreCase) ?? false) && // Exclude machines with 'bootleg' in the name
                !(machine.Element("description")?.Value.Contains("bootleg", StringComparison.InvariantCultureIgnoreCase) ?? false) &&  // Exclude machines with 'bootleg' in the description
                !(machine.Element("description")?.Value.Contains("prototype", StringComparison.InvariantCultureIgnoreCase) ?? false) &&  // Exclude machines with 'prototype' in the description
                !(machine.Element("description")?.Value.Contains("playchoice", StringComparison.InvariantCultureIgnoreCase) ?? false) &&  // Exclude machines with 'playchoice' in the description
                !(machine.Attribute("name")?.Value.Contains("bios", StringComparison.InvariantCultureIgnoreCase) ?? false) &&  // Exclude machines with 'bios' in the name
                !(machine.Element("description")?.Value.Contains("bios", StringComparison.InvariantCultureIgnoreCase) ?? false) &&  // Exclude machines with 'bios' in the description
                (string)(machine.Element("driver").Attribute("emulation")?.Value ?? "") == "good" && // Only add machines with emulation status good
                machine.Attribute("cloneof") == null; // Exclude clones
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Retrieve the matched machines
            var matchedMachines = inputDoc.Descendants("machine").Where(predicate).ToList();

            // Check if any machines matched
            if (matchedMachines.Count == 0)
            {
                Console.WriteLine($"No machines matched for {manufacturer}. Skipping file creation.");
                return;
            }

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

            // Save the filtered XML document
            try
            {
                filteredDoc.Save(outputPath);
                Console.WriteLine($"Successfully created: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create file for {safeManufacturerName}. Error: {ex.Message}");
            }
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
