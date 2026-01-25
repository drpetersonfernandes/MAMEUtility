using System.Xml.Linq;
using MAMEUtility.Interfaces;

namespace MAMEUtility.Services;

public class MameProcessingService(ILogService logService) : IMameProcessingService
{
    private readonly ILogService _logService = logService;

    public async Task CreateMameFullListAsync(
        string inputFilePath,
        string outputFilePath,
        IProgress<int> progress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await MameFull.CreateAndSaveMameFullAsync(inputFilePath, outputFilePath, progress, _logService, cancellationToken);
        }
        catch (System.Xml.XmlException ex)
        {
            await _logService.LogExceptionAsync(ex, $"The XML file is corrupted or truncated. It ended unexpectedly at line {ex.LineNumber}. Please re-download or re-generate the MAME XML file.");
            throw;
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing XML file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameManufacturerListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            await MameManufacturer.CreateAndSaveMameManufacturerAsync(inputFilePath, outputFolderPath, progress, _logService, cancellationToken);
        }
        catch (System.Xml.XmlException ex)
        {
            await _logService.LogExceptionAsync(ex, $"The XML file is corrupted or truncated at line {ex.LineNumber}.");
            throw;
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing manufacturer data from file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameYearListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath), cancellationToken);
            await MameYear.CreateAndSaveMameYearAsync(inputDoc, outputFolderPath, progress, _logService, cancellationToken);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing year data from file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameSourcefileListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath), cancellationToken);
            await MameSourcefile.CreateAndSaveMameSourcefileAsync(inputDoc, outputFolderPath, progress, _logService, cancellationToken);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing sourcefile data from file: {inputFilePath}");
            throw;
        }
    }

    public async Task CreateMameSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            await MameSoftwareList.CreateAndSaveSoftwareListAsync(inputFolderPath, outputFilePath, progress, _logService, cancellationToken);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error creating software list from folder: {inputFolderPath}");
            throw;
        }
    }

    public async Task MergeListsAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            await MergeList.MergeAndSaveBothAsync(inputFilePaths, xmlOutputPath, datOutputPath, progress, _logService, cancellationToken);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error merging lists to {xmlOutputPath} / {datOutputPath}");
            throw;
        }
    }

    public async Task CopyRomsAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            await CopyRoms.CopyRomsFromXmlAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, _logService, cancellationToken);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error copying ROMs from {sourceDirectory} to {destinationDirectory}");
            throw;
        }
    }

    public async Task CopyImagesAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            await CopyImages.CopyImagesFromXmlAsync(xmlFilePaths, sourceDirectory, destinationDirectory, progress, _logService, cancellationToken);
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error copying images from {sourceDirectory} to {destinationDirectory}");
            throw;
        }
    }
}