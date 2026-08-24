using NICE.Platform.FileSync.Worker.Interfaces;
using System.Collections.Concurrent;
using System.Text;

namespace NICE.Platform.FileSync.Worker.Services;

internal class FileLogger : IFileLogger, IDisposable
{
    //public FileLogger(IConfiguration configuration)
    public FileLogger()
    {
        //_logDirectory = configuration["FormsImagesToolService:LogsPath"] ?? string.Empty;
        ////_writeToConsole = bool.Parse(configuration.GetValue<string>("FormsImagesToolService:WriteToConsole") ?? "false");
        //_writeToConsole = bool.Parse("true");
        //_flushIntervalSeconds = int.TryParse(configuration["FormsImagesToolService:LogFlushIntervalSeconds"], out var interval) ? interval : 10;

        _logDirectory = @"C:\logs\dev\FileSyncSvc";
        //_writeToConsole = bool.Parse(configuration.GetValue<string>("FormsImagesToolService:WriteToConsole") ?? "false");
        _writeToConsole = bool.Parse("false");
        _flushIntervalSeconds = 60;

        if (!string.IsNullOrWhiteSpace(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }

        // Start timer to flush logs every interval
        _flushTimer = new Timer(FlushLogs, null, _flushIntervalSeconds * 1000, _flushIntervalSeconds * 1000);
    }
    private readonly string _logDirectory = string.Empty;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly bool _writeToConsole = true;
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly Timer? _flushTimer;
    private readonly int _flushIntervalSeconds = 10;
    private bool _disposed = false;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _flushTimer?.Dispose();
        FlushLogs(null); // Final flush

    }

    public void Log(string message, bool isError = false)
    {
        string level = isError ? "Error" : "Info";

        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - [{System.Environment.MachineName}] [{level}] - {message}{Environment.NewLine}";

        _logQueue.Enqueue(logEntry);

        if (_writeToConsole)
        {
            Console.Write(logEntry);
        }
    }
    private void FlushLogs(object? state)
    {
        // If log directory is not set or there are no logs to flush, exit early
        if (string.IsNullOrEmpty(_logDirectory) || _logQueue.IsEmpty)
            return;

        // Get the current hour and round down to the nearest segment: 0, 6, 12, or 18
        int hour = DateTime.Now.Hour;
        int roundedHour = (hour < 6) ? 0
            : (hour < 12) ? 6
            : (hour < 18) ? 12
            : 18;

        // Build the log file name with date and rounded hour
        string logFileName = $"log-{DateTime.Now:yyyy-MM-dd}-{roundedHour:00}.txt";
        string logFilePath = Path.Combine(_logDirectory, logFileName);

        // Ensure only one thread writes logs at a time
        _semaphore.Wait();
        try
        {
            var sb = new StringBuilder();
            // Dequeue all log entries and append to the StringBuilder
            while (_logQueue.TryDequeue(out var entry))
            {
                sb.Append(entry);
            }

            // If there are logs to write, append them to the log file
            if (sb.Length > 0)
            {
                File.AppendAllText(logFilePath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Swallow any file IO exceptions to avoid crashing the logger
        }
        finally
        {
            // Release the semaphore so other threads can write logs
            _semaphore.Release();
        }
    }
}
