namespace MAMEUtility.Services.Interfaces;

public interface IMameProcessingService
{
    Task CreateMameFullListAsync(string inputFilePath, string outputFilePath, IProgress<int> progress);

    Task CreateMameManufacturerListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress);

    Task CreateMameYearListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress);

    Task CreateMameSourcefileListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress);

    Task CreateMameSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress);

    Task MergeListsAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath);

    Task CopyRomsAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress);

    Task CopyImagesAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress);
}