namespace FolderSynchronizer;

public static class Logger
{
    private static string? logFilePath;
    private static bool isInitialized;

    public enum logType
    {
        Info,
        Warning,
        Error
    }
    public static void Initialize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Log file path is empty");

        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Log file path does not found: {path}");

        logFilePath = path;
        isInitialized = true;
    }

    public static void addLog(string message, logType type)
    {
        EnsureInitialized();
        var logContent = $"[{type}] {DateTime.Now:HH:mm:ss} {message}";

        Console.WriteLine(logContent);
        using (StreamWriter sw = File.AppendText(logFilePath))
        {
            sw.WriteLine(logContent);
        }
    }
    private static void EnsureInitialized()
    {
        if (!isInitialized)
            throw new InvalidOperationException(
                "Logger is not initialized. Call Logger.Initialize(path) first.");
    }
}