using System.Xml.Linq;

namespace MameUtility;

public static class MergeList
{
    public static void MergeAndSave(string[] inputFilePaths, string outputFilePath)
    {
        XDocument mergedDoc = new(new XElement("Machines"));

        foreach (var inputFilePath in inputFilePaths)
        {
            try
            {
                var inputDoc = XDocument.Load(inputFilePath);

                // Validate and normalize the document structure before merging
                if (!IsValidAndNormalizeStructure(inputDoc, out XElement? normalizedRoot))
                {
                    Console.WriteLine($"The file {inputFilePath} does not have the correct XML structure and will not be merged. Operation stopped.");
                    return; // Stop processing further files
                }

                // Merge normalized content
                if (normalizedRoot != null) mergedDoc = MergeDocuments(mergedDoc, normalizedRoot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while loading the file {inputFilePath}: {ex.Message}");
                return; // Stop processing if there's an error loading a file
            }
        }

        mergedDoc.Save(outputFilePath);
        Console.WriteLine($"Merged XML saved successfully to: {outputFilePath}");
    }

    private static bool IsValidAndNormalizeStructure(XDocument doc, out XElement? normalizedRoot)
    {
        normalizedRoot = null;

        // Check for Machines format
        if (doc.Root?.Name.LocalName == "Machines" && doc.Root.Elements("Machine").Any())
        {
            normalizedRoot = doc.Root;
            return true;
        }

        // Check for Softwares format
        if (doc.Root?.Name.LocalName == "Softwares" && doc.Root.Elements("Software").Any())
        {
            // Normalize Softwares to Machines format
            normalizedRoot = new XElement("Machines",
                doc.Root.Elements("Software").Select(software =>
                    new XElement("Machine",
                        new XElement("MachineName", software.Element("SoftwareName")?.Value),
                        software.Element("Description")
                    )
                )
            );
            return true;
        }

        // Invalid structure
        return false;
    }

    private static XDocument MergeDocuments(XDocument doc1, XElement normalizedRoot)
    {
        // Ensure that the first document has a non-null Root element before attempting to merge.
        if (doc1.Root == null)
        {
            throw new InvalidOperationException("The first document does not have a root element.");
        }

        // Add elements from normalizedRoot to the first document
        doc1.Root.Add(normalizedRoot.Elements());

        return doc1;
    }
}
