using System;
using System.IO;
using System.Linq;

namespace SwiftXP.SPT.Common.Environment;

#if NET9_0_OR_GREATER
[SPTarkov.DI.Annotations.Injectable(SPTarkov.DI.Annotations.InjectionType.Singleton)]
#endif
public class BaseDirectoryLocator : IBaseDirectoryLocator
{
    private readonly string _startPath;
    private readonly Lazy<string> _cachedBaseDirectory;

    public BaseDirectoryLocator() : this(AppContext.BaseDirectory) { }

    internal BaseDirectoryLocator(string startPath)
    {
        _startPath = startPath;
        _cachedBaseDirectory = new Lazy<string>(ResolvePath);
    }

    public string GetBaseDirectory() => _cachedBaseDirectory.Value;

    private string ResolvePath()
    {
        string startDirectory = Path.GetFullPath(_startPath);

        DirectoryInfo? current = new(startDirectory);

        while (current != null)
        {
            if (current.GetFiles("EscapeFromTarkov.exe", SearchOption.TopDirectoryOnly).Length != 0)
                return current.FullName;

            bool isServerDir = current.GetFiles("SPT.Server.*", SearchOption.TopDirectoryOnly)
                .Any(f => f.Name.Equals("SPT.Server.exe", StringComparison.OrdinalIgnoreCase) ||
                          f.Name.Equals("SPT.Server.Linux", StringComparison.OrdinalIgnoreCase) ||
                          f.Name.Equals("SPT.Server.ARM64", StringComparison.OrdinalIgnoreCase));

            if (isServerDir)
            {
                return current.Parent?.FullName
                    ?? throw new DirectoryNotFoundException($"Server found at root, but parent directory is required.");
            }

            current = current.Parent;
        }

        string? result = ScanSpecificDepth(startDirectory, 2);
        if (result != null) return result;

        throw new FileNotFoundException(
            "Could not resolve base directory. Neither 'EscapeFromTarkov.exe' nor server executables were found in the path hierarchy.");
    }

    private static string? ScanSpecificDepth(string path, int maxDepth)
    {
        if (maxDepth < 0)
            return null;

        if (Directory.EnumerateFiles(path, "EscapeFromTarkov.exe").Any())
            return path;

        if (Directory.EnumerateFiles(path, "SPT.Server.*").Any())
            return Directory.GetParent(path)?.FullName;

        try
        {
            foreach (string? dir in Directory.EnumerateDirectories(path))
            {
                string? found = ScanSpecificDepth(dir, maxDepth - 1);

                if (found != null)
                    return found;
            }
        }
        catch (UnauthorizedAccessException) { /* Ignorieren */ }

        return null;
    }
}