using System.Text;

namespace onlineHra.Services;

public class LoggingService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public LoggingService(string logFilePath = "Data/server.log")
    {
        _logFilePath = logFilePath;
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] [{level}] {message}";

        lock (_lock)
        {
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }
    }

    public void LogInfo(string message) => Log(message, LogLevel.Info);
    public void LogWarning(string message) => Log(message, LogLevel.Warning);
    public void LogError(string message) => Log(message, LogLevel.Error);
    public void LogCommand(string username, string command) => Log($"[{username}] executed: {command}", LogLevel.Command);
    public void LogPlayerConnect(string username) => Log($"Player '{username}' connected", LogLevel.Info);
    public void LogPlayerDisconnect(string username) => Log($"Player '{username}' disconnected", LogLevel.Info);
}

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Command
}
