namespace FolderSynchronizer;

public class FolderSynchronizer()
{
    public sealed record SyncConfig(int IntervalSeconds, string sourceFolderPath, string replicaFolderPath, string LogFilePath);

    private static SyncConfig ValidateVariables(string[] args)
    {
        if (args == null || args.Length != 4)
            throw new Exception("Wrong number of arguments");

        if (!int.TryParse(args[0], out int interval))
            throw new ArgumentException("Synchronization interval must be int value");

        if (interval < 1)
            throw new ArgumentException("Synchronization interval cannot be less then 1");

        return new SyncConfig(interval, args[1], args[2], args[3]);
    }

    public static async Task Main(string[] args)
    {
        var config = ValidateVariables(args);
        //var config = new SyncConfig(10, @"D:\Users\simon\Desktop\test\", @"D:\Users\simon\Desktop\replica\", @"D:\Users\simon\Desktop\log.txt");
        Logger.Initialize(config.LogFilePath);

        Synchronizator sync = new Synchronizator(config.replicaFolderPath, config.sourceFolderPath);

        while (true)
        {
            sync.Synchronize();
            await Task.Delay(config.IntervalSeconds * 1000);
        }
    }

}