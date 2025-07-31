namespace MAMEUtility.Interfaces;

public interface IMameProcessingService
{
    Task CreateMameFullListAsync(string inputFilePath, string outputFilePath, IProgress<int> progress, CancellationToken cancellationToken = default);
    Task CreateMameManufacturerListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress, CancellationToken cancellationToken = default);
    Task CreateMameYearListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress, CancellationToken cancellationToken = default);
    Task CreateMameSourcefileListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress, CancellationToken cancellationToken = default);
    Task CreateMameSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress, CancellationToken cancellationToken = default);
    Task MergeListsAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath, IProgress<int> progress, CancellationToken cancellationToken = default);
    Task CopyRomsAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, CancellationToken cancellationToken = default);
    Task CopyImagesAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, CancellationToken cancellationToken = default);
}