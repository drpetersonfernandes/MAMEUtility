using System.ComponentModel;
using System.Windows.Threading;
using System.Xml.Linq;
using MAMEUtility.Services.Interfaces;
using MameUtility;

namespace MAMEUtility.Services;

/// <inheritdoc />
/// <summary>
/// Implementation of the IMameProcessingService interface
/// </summary>
public class MameProcessingService : IMameProcessingService
{
    private readonly ILogService _logService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logService">Log service</param>
    public MameProcessingService(ILogService logService)
    {
        _logService = logService;
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates and saves a full MAME list
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFilePath">Path where to save the output XML file</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task CreateMameFullListAsync(string inputFilePath, string outputFilePath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            // Create a background worker for compatibility with MameFull
            var worker = new BackgroundWorker { WorkerReportsProgress = true };
            worker.ProgressChanged += (_, e) => progress.Report(e.ProgressPercentage);

            await MameFull.CreateAndSaveMameFullAsync(inputDoc, outputFilePath, worker, new LogWindowAdapter(_logService));
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing XML file: {inputFilePath}");
            throw;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates and saves MAME manufacturer lists
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFolderPath">Path to the folder where to save the output XML files</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task CreateMameManufacturerListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            await MameManufacturer.CreateAndSaveMameManufacturerAsync(
                inputDoc,
                outputFolderPath,
                progress,
                new LogWindowAdapter(_logService));
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing manufacturer data from file: {inputFilePath}");
            throw;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates and saves MAME year lists
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFolderPath">Path to the folder where to save the output XML files</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task CreateMameYearListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            await Task.Run(() => MameYear.CreateAndSaveMameYear(
                inputDoc,
                outputFolderPath,
                progress,
                new LogWindowAdapter(_logService)));
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing year data from file: {inputFilePath}");
            throw;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates and saves MAME sourcefile lists
    /// </summary>
    /// <param name="inputFilePath">Path to the input XML file</param>
    /// <param name="outputFolderPath">Path to the folder where to save the output XML files</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task CreateMameSourcefileListsAsync(string inputFilePath, string outputFolderPath, IProgress<int> progress)
    {
        try
        {
            var inputDoc = await Task.Run(() => XDocument.Load(inputFilePath));

            await MameSourcefile.CreateAndSaveMameSourcefileAsync(
                inputDoc,
                outputFolderPath,
                progress,
                new LogWindowAdapter(_logService));
        }
        catch (Exception ex)
        {
            await _logService.LogExceptionAsync(ex, $"Error processing sourcefile data from file: {inputFilePath}");
            throw;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates and saves a MAME software list
    /// </summary>
    /// <param name="inputFolderPath">Path to the folder containing XML files</param>
    /// <param name="outputFilePath">Path where to save the output XML file</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task CreateMameSoftwareListAsync(string inputFolderPath, string outputFilePath, IProgress<int> progress)
    {
        try
        {
            // MameSoftwareList.CreateAndSaveSoftwareList is void, so we need to wrap it in a Task
            var tcs = new TaskCompletionSource();

            MameSoftwareList.CreateAndSaveSoftwareList(
                inputFolderPath,
                outputFilePath,
                progress,
                new LogWindowAdapter(_logService));

            tcs.SetResult();
            return tcs.Task;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Merges multiple XML lists into a single XML and DAT file
    /// </summary>
    /// <param name="inputFilePaths">Paths to the input XML files</param>
    /// <param name="xmlOutputPath">Path where to save the output XML file</param>
    /// <param name="datOutputPath">Path where to save the output DAT file</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task MergeListsAsync(string[] inputFilePaths, string xmlOutputPath, string datOutputPath)
    {
        try
        {
            // MergeList.MergeAndSaveBoth is void, so we need to wrap it in a Task
            var tcs = new TaskCompletionSource();

            MergeList.MergeAndSaveBoth(
                inputFilePaths,
                xmlOutputPath,
                datOutputPath,
                new LogWindowAdapter(_logService));

            tcs.SetResult();
            return tcs.Task;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Copies ROMs based on XML file information
    /// </summary>
    /// <param name="xmlFilePaths">Paths to the XML files containing ROM information</param>
    /// <param name="sourceDirectory">Source directory containing ROMs</param>
    /// <param name="destinationDirectory">Destination directory for ROMs</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task CopyRomsAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress)
    {
        return CopyRoms.CopyRomsFromXmlAsync(
            xmlFilePaths,
            sourceDirectory,
            destinationDirectory,
            progress,
            new LogWindowAdapter(_logService));
    }

    /// <inheritdoc />
    /// <summary>
    /// Copies images based on XML file information
    /// </summary>
    /// <param name="xmlFilePaths">Paths to the XML files containing image information</param>
    /// <param name="sourceDirectory">Source directory containing images</param>
    /// <param name="destinationDirectory">Destination directory for images</param>
    /// <param name="progress">Progress reporter</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task CopyImagesAsync(string[] xmlFilePaths, string sourceDirectory, string destinationDirectory, IProgress<int> progress)
    {
        return CopyImages.CopyImagesFromXmlAsync(
            xmlFilePaths,
            sourceDirectory,
            destinationDirectory,
            progress,
            new LogWindowAdapter(_logService));
    }

    /// <inheritdoc />
    /// <summary>
    /// Adapter to convert ILogService to LogWindow for legacy code
    /// </summary>
    private sealed class LogWindowAdapter(ILogService logService) : LogWindow
    {
        private readonly ILogService _logService = logService;
        private readonly Dispatcher _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        public override void AppendLog(string message)
        {
            // Always use the ILogService to log messages, which handles threading correctly
            _logService.Log(message);
        }
    }
}