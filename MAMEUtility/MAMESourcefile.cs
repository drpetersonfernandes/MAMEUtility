using System.IO;
using System.Xml.Linq;

namespace MAMEUtility
{
    public partial class MAMESourcefile
    {
        public static void CreateAndSaveMAMESourcefile(XDocument inputDoc, string outputFolderMAMESourcefile)
        {
            Console.WriteLine($"Output folder for MAME Sourcefile: {outputFolderMAMESourcefile}");

            try
            {
                // Extract unique source files
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                var sourceFiles = inputDoc.Descendants("machine")
                                          .Select(m => (string)m.Attribute("sourcefile"))
                                          .Distinct()
                                          .Where(s => !string.IsNullOrEmpty(s));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                // Iterate over each source file and create an XML for each
                foreach (var sourceFile in sourceFiles)
                {
                    // Check if the source file name is valid
                    if (string.IsNullOrWhiteSpace(sourceFile))
                    {
                        Console.WriteLine("Skipping invalid source file.");
                        continue; // Skip to the next source file
                    }

                    // Remove the ".cpp" extension from the source file name
                    string safeSourceFileName = Path.GetFileNameWithoutExtension(sourceFile);

                    // Replace or remove invalid characters from the file name
                    safeSourceFileName = ReplaceInvalidFileNameChars(safeSourceFileName);

                    // Construct the output file path
                    string outputFilePath = Path.Combine(outputFolderMAMESourcefile, $"{safeSourceFileName}.xml");

                    // Create and save the filtered document
                    CreateAndSaveFilteredDocument(inputDoc, outputFilePath, sourceFile);
                }

                Console.WriteLine("Data extracted and saved successfully for all source files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private static void CreateAndSaveFilteredDocument(XDocument inputDoc, string outputPath, string sourceFile)
        {
            // Filtering condition based on the source file
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            bool predicate(XElement machine) =>
                (string)machine.Attribute("sourcefile")?.Value == sourceFile;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            // Create a new XML document for machines based on the predicate
            XDocument filteredDoc = new(
                new XElement("Machines",
                    from machine in inputDoc.Descendants("machine")
                    where predicate(machine)
                    select new XElement("Machine",
                        new XElement("MachineName", machine.Attribute("name")?.Value),
                        new XElement("Description", machine.Element("description")?.Value)
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
                Console.WriteLine($"Failed to create file for {sourceFile}. Error: {ex.Message}");
            }
        }

        private static string ReplaceInvalidFileNameChars(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_'); // Replace invalid characters with underscores
            }
            return fileName;
        }
    }
}
