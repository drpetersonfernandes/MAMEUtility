using System.ComponentModel;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;

namespace MAMEUtility.Services;

public class MameProcessingService(ILogService logService) : IMameProcessingService
{
    private readonly ILogService _logService = logService;

    public async Task CreateMameFullListAsync(string inputFilePath, string outputFilePath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            // Create a background worker for compatibility with MameFull
            var worker = new BackgroundWorker { WorkerReportsProgress = true };
            worker.ProgressChanged += (_, e) => progress.Report(e.ProgressPercentage);

            // Pass _logService directly
            await MameFull.CreateAndSaveMameFullAsync(inputDoc, outputFilePath, worker, _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing XML file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameManufacturerListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            await MameManufacturer.CreateAndSaveMameManufacturerAsync(
                inputDoc,
                outputFolderPath,
                progress,
                _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing manufacturer data from file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameYearListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            // Pass _logService directly
            await MameYear.CreateAndSaveMameYearAsync(
                inputDoc,
                outputFolderPath,
                progress,
                _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing year data from file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameSourcefileListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            await MameSourcefile.CreateAndSaveMameSourcefileAsync(
                inputDoc,
                outputFolderPath,
                progress,
                _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing sourcefile data from file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress)
    {
        try
        {
            // Pass _logService directly
            await MameSoftwareList.CreateAndSaveSoftwareListAsync(
                inputFolderPath,
                outputFilePath,
                progress,
                _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error creating software list from folder: {inputFolderPath}");
            throw; // Re-throw to signal failure to the ViewModel
        }
    }

    public async Task MergeListsAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath)
    {
        try
        {
            await MergeList.MergeAndSaveBothAsync(
                inputFilePaths,
                xmlOutputPath,
                datOutputPath,
                _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error merging lists to {xmlOutputPath} / {datOutputPath}");
            throw; // Re-throw to signal failure to the ViewModel
        }
    }

    public async Task CopyRomsAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress)
    {
        try
        {
            await CopyRoms.CopyRomsFromXmlAsync(
                xmlFilePaths,
                sourceDirectory,
                destinationDirectory,
                progress,
                _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error copying ROMs from {sourceDirectory} to {destinationDirectory}");
            throw; // Re-throw to signal failure to the ViewModel
        }
    }

    public async Task CopyImagesAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress)
    {
        try
        {
            await CopyImages.CopyImagesFromXmlAsync(
                xmlFilePaths,
                sourceDirectory,
                destinationDirectory,
                progress,
                _logService);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error copying images from {sourceDirectory} to {destinationDirectory}");
            throw; // Re-throw to signal failure to the ViewModel
        }
    }
}