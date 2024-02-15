using System;
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

            // Ensure the documents have a root before proceeding
            if (doc1.Root == null || doc2.Root == null)
            {
                throw new InvalidOperationException("One or both documents do not have a root element.");
            }

            // Create a new XDocument to hold the merged content
            XDocument mergedDoc = new(new XElement(doc1.Root.Name));

            // Add elements from both documents, asserting Root is not null
            mergedDoc.Root!.Add(doc1.Root.Elements());
            mergedDoc.Root!.Add(doc2.Root.Elements());

            return mergedDoc;
        }
    }
}