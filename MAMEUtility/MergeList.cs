using System.Xml.Linq;

namespace MameUtility
{
    public static class MergeList
    {
        public static void MergeAndSave(string inputFilePath1, string inputFilePath2, string outputFilePath)
        {
            XDocument inputDoc1 = XDocument.Load(inputFilePath1);
            XDocument inputDoc2 = XDocument.Load(inputFilePath2);

            XDocument mergedDoc = MergeDocuments(inputDoc1, inputDoc2);

            mergedDoc.Save(outputFilePath);
            Console.WriteLine($"Merged XML saved successfully to: {outputFilePath}");
        }

        public static XDocument MergeDocuments(XDocument doc1, XDocument doc2)
        {
            // Check if either document is null
            if (doc1 == null && doc2 == null)
            {
                throw new ArgumentNullException(nameof(doc1), "Both documents are null.");
            }
            else if (doc1 == null)
            {
                return new XDocument(doc2);
            }
            else if (doc2 == null)
            {
                return new XDocument(doc1);
            }

            // Create a new XDocument to hold the merged content
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            XDocument mergedDoc = new(new XElement(doc1.Root.Name));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Add elements from the first document
            if (doc1.Root != null)
            {
                mergedDoc.Root?.Add(doc1.Root.Elements());
            }

            // Add elements from the second document
            if (doc2.Root != null)
            {
                mergedDoc.Root?.Add(doc2.Root.Elements());
            }

            return mergedDoc;
        }


    }
}
