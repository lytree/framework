using System;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Framework;
using ZLogger;

namespace Framework.ZLogging;

public class SpectreRollingFileLogProcessorOptions
{
    public string FilePath { get; set; } = "app.log";
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxFileCount { get; set; } = 3;
    public string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public bool UseTime { get; set; } = true;
    public bool EnableAnsi { get; set; } = false;
}

public class SpectreRollingFileLogProcessor : IAsyncLogProcessor, IAsyncDisposable
{
    private readonly string _filePath;
    private readonly int _maxFileSize;
    private readonly int _maxFileCount;
    private readonly Channel<IZLoggerEntry> _channel;
    private readonly Task _writeLoop;
    private StreamWriter _writer;
    private long _currentFileSize;
    private int _fileIndex;
    private readonly SpectreRollingFileLogProcessorOptions _options;

    public SpectreRollingFileLogProcessor(SpectreRollingFileLogProcessorOptions options)
    {
        _options = options;
        _filePath = options.FilePath;
        _maxFileSize = options.MaxFileSizeBytes;
        _maxFileCount = options.MaxFileCount;
        _writer = CreateOrOpenFile();
        _channel = Channel.CreateUnbounded<IZLoggerEntry>();
        _writeLoop = Task.Run(WriteLoop);
    }

    private StreamWriter CreateOrOpenFile()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var fileName = _fileIndex > 0
            ? Path.GetFileNameWithoutExtension(_filePath) + $".{_fileIndex}" + Path.GetExtension(_filePath)
            : _filePath;

        var stream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 1, false);
        return new StreamWriter(stream, Encoding.UTF8);
    }

    private StreamWriter GetCurrentWriter()
    {
        if (_currentFileSize >= _maxFileSize)
        {
            RotateFile();
            _writer.Dispose();
            _writer = CreateOrOpenFile();
            _currentFileSize = 0;
        }
        return _writer;
    }

    private void RotateFile()
    {
        _fileIndex++;
        if (_fileIndex > _maxFileCount)
        {
            _fileIndex = 1;
        }

        var oldFile = _fileIndex > 1
            ? Path.GetFileNameWithoutExtension(_filePath) + $".{_fileIndex}" + Path.GetExtension(_filePath)
            : _filePath;

        if (File.Exists(oldFile))
        {
            File.Delete(oldFile);
        }
    }

    private async Task WriteLoop()
    {
        await foreach (var entry in _channel.Reader.ReadAllAsync())
        {
            try
            {
                var writer = GetCurrentWriter();
                var message = entry.ToString();
                var plainText = Helper.RemoveMarkup(message);
                var output = BuildOutput(plainText);

                await writer.WriteLineAsync(output);
                await writer.FlushAsync();

                _currentFileSize += Encoding.UTF8.GetByteCount(output) + Environment.NewLine.Length;
            }
            finally
            {
                entry.Return();
            }
        }
    }

    private string BuildOutput(string message)
    {
        var timestamp = _options.UseTime ? DateTime.Now.ToString(_options.TimeFormat) + " " : "";
        var logLevel = Helper.GetLogLevel(message);
        var category = Helper.GetLogCategory(message);
        var categoryStr = !string.IsNullOrEmpty(category) ? $"[{category}] " : "";
        return $"{timestamp}{logLevel} {categoryStr}{message}";
    }

    public void Post(IZLoggerEntry log)
    {
        _channel.Writer.TryWrite(log);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        await _writeLoop;
        await _writer.DisposeAsync();
    }
}