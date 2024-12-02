using System.Xml.Linq;

namespace MameUtility
{
    public static class MergeList
    {
        public static void MergeAndSave(string[] inputFilePaths, string outputFilePath)
        {
            XDocument mergedDoc = new(new XElement("Machines"));

            foreach (var inputFilePath in inputFilePaths)
            {
                XDocument inputDoc;
                try
                {
                    inputDoc = XDocument.Load(inputFilePath);

                    // Validate the document structure before merging
                    if (!IsValidStructure(inputDoc))
                    {
                        Console.WriteLine($"The file {inputFilePath} does not have the correct XML structure and will not be merged. Operation stopped.");
                        return; // Stop processing further files
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while loading the file {inputFilePath}: {ex.Message}");
                    return; // Stop processing if there's an error loading a file
                }

                mergedDoc = MergeDocuments(mergedDoc, inputDoc);
            }

            mergedDoc.Save(outputFilePath);
            Console.WriteLine($"Merged XML saved successfully to: {outputFilePath}");
        }

        private static bool IsValidStructure(XDocument doc)
        {
            // Check if the root element is "Machines" and if it contains at least one "Machine" child element
            return doc.Root?.Name.LocalName == "Machines" && doc.Root.Elements("Machine").Any();
        }

        private static XDocument MergeDocuments(XDocument doc1, XDocument doc2)
        {
            // Ensure that both documents have a non-null Root element before attempting to merge.
            if (doc1.Root == null)
            {
                throw new InvalidOperationException("The first document does not have a root element.");
            }

            if (doc2.Root == null)
            {
                throw new InvalidOperationException("The second document does not have a root element.");
            }

            // Now that we've ensured the Root elements are not null, it's safe to proceed.
            doc1.Root.Add(doc2.Root.Elements());

            return doc1;
        }

    }
}