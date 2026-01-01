namespace FolderSynchronizer;

public static class Logger
{
    private static string? logFilePath;
    private static bool isInitialized;
    private static readonly object syncRoot = new();

    public enum LogType
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
        if (string.IsNullOrEmpty(dir))
            throw new DirectoryNotFoundException($"Log file directory not found: {path}");

        Directory.CreateDirectory(dir);

        logFilePath = path;
        isInitialized = true;
    }

    public static void AddLog(string message, LogType type)
    {
        EnsureInitialized();
        var logContent = $"[{type}] {DateTime.Now:HH:mm:ss} {message}";

        lock (syncRoot)
        {
            Console.WriteLine(logContent);
            using (StreamWriter sw = File.AppendText(logFilePath))
            {
                sw.WriteLine(logContent);
            }
        }
    }

    private static void EnsureInitialized()
    {
        if (!isInitialized)
            throw new InvalidOperationException(
                "Logger is not initialized. Call Logger.Initialize(path) first.");
    }
}
