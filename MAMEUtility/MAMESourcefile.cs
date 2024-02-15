﻿using System.IO;
using System.Xml.Linq;

namespace MAMEUtility
{
    public partial class MAMESourcefile
    {
        public static async Task CreateAndSaveMAMESourcefileAsync(XDocument inputDoc, string outputFolderMAMESourcefile, IProgress<int> progress)
        {
            Console.WriteLine($"Output folder for MAME Sourcefile: {outputFolderMAMESourcefile}");

            try
            {
                // Extract unique source files
                var sourceFiles = inputDoc.Descendants("machine")
                    .Select(m => (string?)m.Attribute("sourcefile"))
                    .Distinct()
                    .Where(s => !string.IsNullOrEmpty(s));

                int totalSourceFiles = sourceFiles.Count();
                int sourceFilesProcessed = 0;

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
                    await CreateAndSaveFilteredDocumentAsync(inputDoc, outputFilePath, sourceFile);

                    sourceFilesProcessed++;
                    double progressPercentage = (double)sourceFilesProcessed / totalSourceFiles * 100;
                    progress.Report((int)progressPercentage);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private static async Task CreateAndSaveFilteredDocumentAsync(XDocument inputDoc, string outputPath, string sourceFile)
        {
            // Filtering condition based on the source file
            bool predicate(XElement machine) =>
                (string?)machine.Attribute("sourcefile") == sourceFile;

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
                await Task.Run(() => filteredDoc.Save(outputPath));
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
                fileName = fileName.Replace(invalidChar, '_');
            }
            return fileName;
        }
    }
}