namespace MAMEUtility.Services.Interfaces;

/// <summary>
/// Service for processing MAME data
/// </summary>
public interface IMameProcessingService
{
    /// <summary>
    /// Creates and saves a full MAME list
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFilePath">Path where to save the output XML file</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CreateMameFullListAsync(string inputFilePath, string outputFilePath, IProgress<int> progress);

    /// <summary>
    /// Creates and saves MAME manufacturer lists
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFolderPath">Path to the folder where to save the output XML files</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CreateMameManufacturerListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress);

    /// <summary>
    /// Creates and saves MAME year lists
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFolderPath">Path to the folder where to save the output XML files</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CreateMameYearListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress);

    /// <summary>
    /// Creates and saves MAME sourcefile lists
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFolderPath">Path to the folder where to save the output XML files</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CreateMameSourcefileListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress);

    /// <summary>
    /// Creates and saves a MAME software list
    /// </summary>
    /// <param name="inputFolderPath">Path to the folder containing XML files</param>
    /// <param name="outputFilePath">Path where to save the output XML file</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CreateMameSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress);

    /// <summary>
    /// Merges multiple XML lists into a single XML and DAT file
    /// </summary>
    /// <param name="inputFilePaths">Paths to the input XML files</param>
    /// <param name="xmlOutputPath">Path where to save the output XML file</param>
    /// <param name="datOutputPath">Path where to save the output DAT file</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task MergeListsAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath);

    /// <summary>
    /// Copies ROMs based on XML file information
    /// </summary>
    /// <param name="xmlFilePaths">Paths to the XML files containing ROM information</param>
    /// <param name="sourceDirectory">Source directory containing ROMs</param>
    /// <param name="destinationDirectory">Destination directory for ROMs</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CopyRomsAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress);

    /// <summary>
    /// Copies images based on XML file information
    /// </summary>
    /// <param name="xmlFilePaths">Paths to the XML files containing image information</param>
    /// <param name="sourceDirectory">Source directory containing images</param>
    /// <param name="destinationDirectory">Destination directory for images</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CopyImagesAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress);
}