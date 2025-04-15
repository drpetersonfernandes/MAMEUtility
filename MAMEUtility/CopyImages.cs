using System.IO;
using System.Xml.Linq;

namespace MAMEUtility;

public static class CopyImages
{
    public static async Task CopyImagesFromXmlAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, LogWindow logWindow)
    {
        var totalFiles = xmlFilePaths.Length;
        var filesCopied = 0;

        // Define logging intervals
        const int logInterval = 5; // Log progress every 5 files
        const int progressInterval = 2; // Update progress every 2 files

        await LogError.LogMessageAsync($"Starting image copy operation. Files to process: {totalFiles}");
        await LogError.LogMessageAsync($"Source directory: {sourceDirectory}");
        await LogError.LogMessageAsync($"Destination directory: {destinationDirectory}");

        // Validate directories exist
        if (!Directory.Exists(sourceDirectory))
        {
            await LogError.LogMessageAsync($"Source directory does not exist: {sourceDirectory}", LogLevel.Error);
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirectory}");
        }

        // Create destination directory if it doesn't exist
        if (!Directory.Exists(destinationDirectory))
        {
            try
            {
                Directory.CreateDirectory(destinationDirectory);
                await LogError.LogMessageAsync($"Created destination directory: {destinationDirectory}");
            }
            catch (Exception ex)
            {
                await LogError.LogAsync(ex, $"Failed to create destination directory: {destinationDirectory}");
                throw;
            }
        }

        // Process each XML file - sequential to avoid overwhelming file system
        foreach (var xmlFilePath in xmlFilePaths)
        {
            try
            {
                await ProcessXmlFileAsync(xmlFilePath, sourceDirectory, destinationDirectory, progress, logWindow);

                filesCopied++;

                // Log progress at intervals
                if (filesCopied % logInterval == 0 || filesCopied == totalFiles)
                {
                    await LogError.LogMessageAsync($"Progress: {filesCopied}/{totalFiles} XML files processed.");
                }

                // Update progress bar at intervals
                if (filesCopied % progressInterval != 0 && filesCopied != totalFiles) continue;

                var progressPercentage = (double)filesCopied / totalFiles * 100;
                progress.Report((int)progressPercentage);
            }
            catch (Exception ex)
            {
                await LogError.LogAsync(ex, $"An error occurred processing {Path.GetFileName(xmlFilePath)}");
                logWindow.AppendLog($"An error occurred processing {Path.GetFileName(xmlFilePath)}: {ex.Message}");
            }
        }

        await LogError.LogMessageAsync($"Image copy operation completed. Processed {filesCopied} of {totalFiles} files.");
    }

    // Fix for the ProcessXmlFileAsync method in CopyImages.cs
    // Fix for the ProcessXmlFileAsync method in CopyImages.cs
    private static async Task ProcessXmlFileAsync(string xmlFilePath, string sourceDirectory, string destinationDirectory, IProgress<int> progress, LogWindow logWindow)
    {
        XDocument xmlDoc;

        try
        {
            xmlDoc = await Task.Run(() => XDocument.Load(xmlFilePath));
            await LogError.LogMessageAsync($"Successfully loaded XML file: {Path.GetFileName(xmlFilePath)}");
        }
        catch (Exception ex)
        {
            await LogError.LogAsync(ex, $"Failed to load XML file: {xmlFilePath}");
            throw;
        }

        // Validate the XML document structure
        if (!ValidateXmlStructure(xmlDoc))
        {
            var message = $"The file {Path.GetFileName(xmlFilePath)} does not match the required XML structure. Operation cancelled.";
            await LogError.LogMessageAsync(message, LogLevel.Warning);
            logWindow.AppendLog(message);
            return;
        }

        var machineNames = xmlDoc.Descendants("Machine")
            .Select(static machine => machine.Element("MachineName")?.Value)
            .Where(static name => !string.IsNullOrEmpty(name))
            .ToList();

        var totalImages = machineNames.Count;
        var imagesCopied = 0;

        // Define logging intervals for internal processing
        const int internalLogInterval = 100; // Log every 100 images
        const int internalProgressInterval = 50; // Update progress every 50 images

        await LogError.LogMessageAsync($"Found {totalImages} machine entries in {Path.GetFileName(xmlFilePath)}");

        // Process images with controlled parallelism
        var maxConcurrency = Math.Max(1, Environment.ProcessorCount);
        var activeTasks = new List<Task>();
        var processingQueue = new Queue<string>(machineNames!);

        // Start initial batch of tasks
        while (activeTasks.Count < maxConcurrency && processingQueue.Count > 0)
        {
            var machine = processingQueue.Dequeue();
            activeTasks.Add(ProcessMachineAsync(machine));
        }

        // Process remaining machines as tasks complete
        while (activeTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(activeTasks);
            activeTasks.Remove(completedTask);

            // Add a new task if there are items remaining
            if (processingQueue.Count <= 0) continue;

            var machine = processingQueue.Dequeue();
            activeTasks.Add(ProcessMachineAsync(machine));
        }

        await LogError.LogMessageAsync($"Completed processing {imagesCopied} machines from {Path.GetFileName(xmlFilePath)}");
        return;

        // Helper function to process a machine
        async Task ProcessMachineAsync(string machineName)
        {
            try
            {
                await CopyImageFileAsync(sourceDirectory, destinationDirectory, machineName, "png", logWindow);
                await CopyImageFileAsync(sourceDirectory, destinationDirectory, machineName, "jpg", logWindow);
                await CopyImageFileAsync(sourceDirectory, destinationDirectory, machineName, "jpeg", logWindow);

                // Thread-safe incrementing and reporting
                var processed = Interlocked.Increment(ref imagesCopied);

                // Update progress at intervals
                if (processed % internalProgressInterval == 0 || processed == totalImages)
                {
                    var progressPercentage = (double)processed / totalImages * 100;
                    progress.Report((int)progressPercentage);
                }

                // Log at intervals
                if (processed % internalLogInterval == 0 || processed == totalImages)
                {
                    await LogError.LogMessageAsync($"Image copy progress: {processed}/{totalImages} from {Path.GetFileName(xmlFilePath)}");
                }
            }
            catch (Exception ex)
            {
                // Log any errors but continue processing
                await LogError.LogAsync(ex, $"Error processing {machineName}");
                logWindow.AppendLog($"Error processing {machineName}: {ex.Message}");
            }
        }
    }


    private static async Task CopyImageFileAsync(string sourceDirectory, string destinationDirectory, string? machineName, string extension, LogWindow logWindow)
    {
        if (machineName == null)
        {
            await LogError.LogMessageAsync($"Machine name is null for extension: {extension}", LogLevel.Warning);
            logWindow.AppendLog($"Machine name is null for extension: {extension}");
            return;
        }

        var sourceFile = Path.Combine(sourceDirectory, machineName + "." + extension);
        var destinationFile = Path.Combine(destinationDirectory, machineName + "." + extension);

        await Task.Run(async () =>
        {
            if (File.Exists(sourceFile))
            {
                try
                {
                    File.Copy(sourceFile, destinationFile, true);
                    logWindow.AppendLog($"Copied: {machineName}.{extension} to {destinationDirectory}");
                }
                catch (Exception ex)
                {
                    await LogError.LogAsync(ex, $"Failed to copy {machineName}.{extension}");
                    logWindow.AppendLog($"Failed to copy {machineName}.{extension}: {ex.Message}");
                }
            }
            else
            {
                // This is just informational, doesn't need to be logged as a warning
                logWindow.AppendLog($"File not found: {machineName}.{extension}");
            }
        });
    }

    private static bool ValidateXmlStructure(XDocument xmlDoc)
    {
        // Check if the root element is "Machines" and if it contains at least one "Machine" element
        // with both "MachineName" and "Description" child elements.
        var isValid = xmlDoc.Root?.Name.LocalName == "Machines" &&
                      xmlDoc.Descendants("Machine").Any(static machine =>
                          machine.Element("MachineName") != null &&
                          machine.Element("Description") != null);

        return isValid;
    }
}