namespace FolderSynchronizer;

public class FilesSearcher
{
    public static (List<string> Files, List<string> Directories) GetFilesAndDirectories(
        string rootPath,
        string searchPattern = "*",
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Path is empty.", nameof(rootPath));

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");

        var files = new List<string>();
        var dirs = new List<string>();

        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            var currentFolder = stack.Pop();
            try
            {
                var filesPaths = Directory.GetFiles(
                    currentFolder,
                    searchPattern,
                    SearchOption.TopDirectoryOnly);
                files.AddRange(filesPaths);

                foreach (string filePath in Directory.GetDirectories(currentFolder))
                {
                    dirs.Add(filePath);

                    if (searchOption == SearchOption.AllDirectories)
                    {
                        stack.Push(filePath);
                    }
                }
            }
            catch (Exception e) when (
                e is UnauthorizedAccessException ||
                e is DirectoryNotFoundException ||
                e is PathTooLongException ||
                e is IOException)
            {
                Logger.AddLog(e.ToString(), Logger.LogType.Error);
            }
        }

        return (files, dirs);
    }
}
