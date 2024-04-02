using System.IO;
using MessagePack;
using NovelNode.Helpers;


namespace NovelNodePlayer.Data;

public class GameResources
{
    private Dictionary<string, (long position, long length)>? _index;
    private Dictionary<string, byte[]>? _data;
    private string _inputPath = string.Empty;

    public async Task PackDirectory(string sourceDirectory, string outputPath, bool isProject = false)
    {
        var files = Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);

        if (isProject)
        {
            Directory.CreateDirectory(outputPath);
            foreach (var dirPath in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourceDirectory, outputPath));

            foreach (var file in files)
            {
                string targetPath = Path.Combine(outputPath, file[(sourceDirectory.Length + 1)..]);
                var sourceStream = file.GetFileStream(true);
                var targetStream = targetPath.GetFileStream(true);
                await sourceStream.CopyToAsync(targetStream);
            }
        }
        else
        {
            var filesData = new List<(string, byte[])>();
            var directoryIndex = new Dictionary<string, (long position, long length)>();

            await Task.Run(async () =>
            {
                long position = 0;
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(sourceDirectory, file);
                    var fileData = await file.ReadBytesAsync();
                    filesData.Add((relativePath, fileData));
                    directoryIndex[relativePath] = (position, fileData.Length);
                    position += fileData.Length;
                }
            });


            using var stream = new FileStream($"{outputPath}\\GameResources.novelnode", FileMode.Create, FileAccess.Write, FileShare.None);
            await MessagePackSerializer.SerializeAsync(stream, new { FilesData = filesData, DirectoryIndex = directoryIndex });
            await stream.FlushAsync();
        }
    }

    public async Task LoadIndex(string inputPath, bool loadFull = false, IProgress<double>? progress = null)
    {
        _inputPath = inputPath;

        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        var fileSize = stream.Length;
        var bytesRead = 0L;
        var buffer = new byte[4096];
        int bytesReadThisChunk;
        double lastReportedProgress = 0;

        void ReportProgress()
        {
            if (progress != null && fileSize > 0)
            {
                var currentProgress = (double)bytesRead / fileSize * 100;
                var currentProgressRounded = Math.Floor(currentProgress);

                if (currentProgressRounded > lastReportedProgress)
                {
                    progress.Report(currentProgressRounded);
                    lastReportedProgress = currentProgressRounded;
                }
            }
        }

        while ((bytesReadThisChunk = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            bytesRead += bytesReadThisChunk;
            ReportProgress();
        }

        // Reset the stream position to the beginning
        stream.Seek(0, SeekOrigin.Begin);

        try
        {
            var unpackedData = await MessagePackSerializer.DeserializeAsync<Dictionary<string, object>>(stream);
            bytesRead = stream.Position;
            ReportProgress();

            if (!unpackedData.TryGetValue("DirectoryIndex", out var directoryIndexObj) || directoryIndexObj is not Dictionary<object, object> directoryIndexDict)
            {
                throw new InvalidDataException("DirectoryIndex not found or invalid format in packed file");
            }

            _index = directoryIndexDict.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp =>
                {
                    var tuple = kvp.Value as IList<object>;
                    if (tuple == null || tuple.Count != 2)
                    {
                        throw new InvalidDataException("Invalid format for DirectoryIndex tuple in packed file");
                    }
                    return (Convert.ToInt64(tuple[0]), Convert.ToInt64(tuple[1]));
                });

            if (loadFull && unpackedData.TryGetValue("FilesData", out var filesDataObj) && filesDataObj is object[] filesDataArray)
            {
                _data = filesDataArray.ToDictionary(
                    item => (string)((object[])item)[0],
                    item => (byte[])((object[])item)[1]);
            }
            else if (loadFull)
            {
                throw new InvalidDataException("FilesData not found or invalid format in packed file");
            }
        }
        catch (Exception ex)
        {
            Extensions.Notify("Resources", $"An error occurred while loading the index from '{_inputPath}'", Notification.Wpf.NotificationType.Error, ex: ex);
            throw;
        }
    }

    public List<string> GetDirectory(string directoryPath)
    {
        try
        {
            if (_index == null)
                throw new InvalidOperationException("Index has not been loaded. Call LoadIndex first.");

            var files = new List<string>();

            foreach (var entry in _index)
            {
                if (entry.Key.StartsWith(directoryPath))
                    files.Add(entry.Key);
            }

            return files;
        }
        catch (Exception ex)
        {
            Extensions.Notify("Resources", $"An error occurred while getting directory '{directoryPath}'", Notification.Wpf.NotificationType.Error, ex: ex);
            throw;
        }
    }

    public async Task<byte[]> GetFile(string relativePath, bool streaming = false)
    {
        try
        {
            if (_index == null || _data == null || string.IsNullOrEmpty(relativePath))
                throw new InvalidOperationException("Index is not loaded or relative path is not set");

            if (streaming)
            {
                if (_index.TryGetValue(relativePath, out var fileIndex))
                {
                    using var stream = new FileStream(_inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

                    // Validate file index position
                    if (fileIndex.position < 0 || fileIndex.position >= stream.Length)
                        throw new InvalidDataException($"Invalid position for file '{relativePath}' in packed file");
                    // Validate file index length
                    if (fileIndex.length <= 0 || fileIndex.length > stream.Length - fileIndex.position)
                        throw new InvalidDataException($"Invalid length for file '{relativePath}' in packed file");

                    // Seek to the position of the file data
                    stream.Seek(fileIndex.position, SeekOrigin.Begin);

                    // Read only the specified length of bytes for the file data
                    var fileData = new byte[fileIndex.length];
                    await stream.ReadAsync(fileData.AsMemory(0, (int)fileIndex.length));

                    return fileData;
                }
            }
            else
            {
                if (_data.TryGetValue(relativePath, out byte[]? value))
                    return value;
            }

            throw new FileNotFoundException($"File '{relativePath}' not found in packed file");
        }
        catch (Exception ex)
        {
            Extensions.Notify("Resources", $"An error occurred while getting file '{relativePath}'", Notification.Wpf.NotificationType.Error, ex: ex);
            throw;
        }
    }

    public async Task UnpackDirectory(string outputDirectory)
    {
        try
        {
            if (_index == null || string.IsNullOrEmpty(_inputPath))
                throw new InvalidOperationException("Index is not loaded or input path is not set");

            // Ensure the output directory exists
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            foreach (var item in _data)
            {
                var outputPath = Path.Combine(outputDirectory, item.Key);
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                await outputPath.WriteBytesAsync(item.Value);
            }
        }
        catch (Exception ex)
        {
            Extensions.Notify("Resources", "An error occurred while unpacking directory", Notification.Wpf.NotificationType.Error, ex: ex);
            throw;
        }
    }

    #region Statics
    private static GameResources? instance;
    public static GameResources Instance => instance ??= new GameResources();
    #endregion
}
