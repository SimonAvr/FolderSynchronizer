using System.Security.Cryptography;
using FolderSynchronizer;

public class Synchronizator
{
    private readonly string replicaRootPath;
    private readonly string sourceRootPath;

    private sealed record Snapshot(
        string Root,
        Dictionary<string, string> FilesPaths,
        Dictionary<string, string> DictionariesPaths
    );

    private string ToSourceFull(string rel) => Path.Combine(sourceRootPath, rel);
    private string ToReplicaFull(string rel) => Path.Combine(replicaRootPath, rel);

    public Synchronizator(string replicaRootPath, string sourceRootPath)
    {
        this.replicaRootPath = ValidateDir(replicaRootPath, nameof(replicaRootPath));
        this.sourceRootPath = ValidateDir(sourceRootPath, nameof(sourceRootPath));
    }

    private static string ValidateDir(string path, string paramName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty.", paramName);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory does not exist: {path}");

        return Path.GetFullPath(path);
    }

    public void Synchronize()
    {
        Logger.addLog("Synchronization started", Logger.logType.Info);
        var source = GetSnapshot(sourceRootPath);
        var replica = GetSnapshot(replicaRootPath);

        // Directories
        var dirsToCreate = source.DictionariesPaths.Keys.Except(replica.DictionariesPaths.Keys);
        var dirsToDelete = replica.DictionariesPaths.Keys.Except(source.DictionariesPaths.Keys);

        CreateDirectories(dirsToCreate);

        // Files
        var filesToCopy = source.FilesPaths.Keys.Except(replica.FilesPaths.Keys);
        var filesToDelete = replica.FilesPaths.Keys.Except(source.FilesPaths.Keys);
        var filesToMaybeUpdate = source.FilesPaths.Keys.Intersect(replica.FilesPaths.Keys);

        CopyFiles(filesToCopy);
        UpdateFiles(filesToMaybeUpdate);
        DeleteFiles(filesToDelete);

        // Delete directories last
        DeleteDirectories(dirsToDelete);
        Logger.addLog("Synchronization performed successfully", Logger.logType.Info);
    }

    private Snapshot GetSnapshot(string rootPath)
    {
        var (files, dirs) = FilesSearcher.GetFilesAndDirectories(rootPath);

        var fileMap = files.ToDictionary(full => Path.GetRelativePath(
            rootPath, full),
            full => full,
            StringComparer.OrdinalIgnoreCase);

        var dirsMap = dirs.ToDictionary(full => Path.GetRelativePath(
            rootPath, full),
            full => full,
            StringComparer.OrdinalIgnoreCase);

        return new Snapshot(rootPath, fileMap, dirsMap);
    }


    private void CreateDirectories(IEnumerable<string> relDirs)
    {
        foreach (var rel in relDirs.OrderBy(r => r.Count(c => c == Path.DirectorySeparatorChar)))// shallow -> deep
        {
            Try(() =>
            {
                var dst = ToReplicaFull(rel);
                Directory.CreateDirectory(dst);
                Logger.addLog($"Directory created: {dst}", Logger.logType.Info);
            }, $"Create directory: {rel}");
        }
    }

    private void DeleteDirectories(IEnumerable<string> relDirs)
    {
        foreach (var rel in relDirs.OrderByDescending(r => r.Count(c => c == Path.DirectorySeparatorChar)))// deep -> shallow
        {
            Try(() =>
            {
                var dst = ToReplicaFull(rel);
                Directory.Delete(dst);
                Logger.addLog($"Directory deleted: {dst}", Logger.logType.Info);
            }, $"Delete directory: {rel}");
        }
    }

    private void CopyFiles(IEnumerable<string> relFiles)
    {
        foreach (var rel in relFiles)
        {
            Try(() =>
            {
                var src = ToSourceFull(rel);
                var dst = ToReplicaFull(rel);

                File.Copy(src, dst, overwrite: true);

                Logger.addLog($"File copied: {src} -> {dst}", Logger.logType.Info);
            }, $"Copy file: {rel}");
        }
    }

    private void DeleteFiles(IEnumerable<string> relFiles)
    {
        foreach (var rel in relFiles)
        {
            Try(() =>
            {
                var dst = ToReplicaFull(rel);
                if (File.Exists(dst))
                {
                    File.Delete(dst);
                    Logger.addLog($"File removed: {dst}", Logger.logType.Info);
                }
            }, $"Delete file: {rel}");
        }
    }

    private void UpdateFiles(IEnumerable<string> relFiles)
    {
        foreach (var rel in relFiles)
        {
            Try(() =>
            {
                var src = ToSourceFull(rel);
                var dst = ToReplicaFull(rel);

                if (!File.Exists(src) || !File.Exists(dst))
                    return;

                // Fast check first
                var sInfo = new FileInfo(src);
                var rInfo = new FileInfo(dst);

                if (sInfo.Length == rInfo.Length &&
                    sInfo.LastWriteTimeUtc == rInfo.LastWriteTimeUtc)
                    return;

                // Strong check (only when needed)
                if (GetFileMd5(src) == GetFileMd5(dst))
                    return;

                File.Copy(src, dst, overwrite: true);

                // keep timestamps in sync
                File.SetLastWriteTimeUtc(dst, sInfo.LastWriteTimeUtc);

                Logger.addLog($"File updated: {src} -> {dst}", Logger.logType.Info);
            }, $"Update file: {rel}");
        }
    }

    private static void Try(Action action, string context)
    {
        try { action(); }
        catch (Exception e)
        {
            Logger.addLog($"{context}. {e}", Logger.logType.Error);
        }
    }

    private static string GetFileMd5(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = md5.ComputeHash(stream);
        return Convert.ToHexString(hashBytes);
    }
}
