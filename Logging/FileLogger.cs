using System.Text;
using System.IO;

namespace Visual_FloydWarshall.Logging;

public sealed class FileLogger(string filePath) : ILogger
{
    private readonly object _syncRoot = new();

    public void Info(string message) => Write("INFO", message);

    public void Error(string message, Exception? exception = null)
    {
        var finalMessage = exception is null
            ? message
            : $"{message}{Environment.NewLine}{exception}";

        Write("ERROR", finalMessage);
    }

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";

        lock (_syncRoot)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.AppendAllText(filePath, line, Encoding.UTF8);
        }
    }
}
