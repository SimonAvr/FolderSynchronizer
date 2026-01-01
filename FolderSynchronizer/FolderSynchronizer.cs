using System;
using System.Threading;

namespace FolderSynchronizer;

public class FolderSynchronizer()
{
    public sealed record SyncConfig(int IntervalSeconds, string SourceFolderPath, string ReplicaFolderPath, string LogFilePath);

    private static SyncConfig ValidateVariables(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length != 4)
            throw new ArgumentException("Expected 4 arguments: intervalSeconds sourceFolderPath replicaFolderPath logFilePath.", nameof(args));

        if (!int.TryParse(args[0], out int interval))
            throw new ArgumentException("Synchronization interval must be int value");

        if (interval < 1)
            throw new ArgumentOutOfRangeException(nameof(args), "Synchronization interval cannot be less than 1");

        return new SyncConfig(interval, args[1], args[2], args[3]);
    }

    public static async Task Main(string[] args)
    {
        try
        {
            var config = ValidateVariables(args);
            Logger.Initialize(config.LogFilePath);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Logger.AddLog("Cancellation requested. Exiting...", Logger.LogType.Info);
            };

            Synchronizator sync = new Synchronizator(config.ReplicaFolderPath, config.SourceFolderPath);

            while (!cts.IsCancellationRequested)
            {
                sync.Synchronize();
                await Task.Delay(TimeSpan.FromSeconds(config.IntervalSeconds), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            try
            {
                Logger.AddLog($"Fatal error: {ex}", Logger.LogType.Error);
            }
            catch
            {
                // Swallow logging errors in fatal path
            }
            Environment.ExitCode = 1;
        }
    }

}
