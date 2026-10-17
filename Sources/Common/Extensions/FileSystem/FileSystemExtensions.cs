using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace SwiftXP.SPT.Common.Extensions.FileSystem;

public static class FileSystemExtensions
{
    public static IEnumerable<FileInfo> FindFilesByPattern(this string baseDirectory,
        IEnumerable<string> includePatterns, IEnumerable<string>? excludePatterns = null)
    {
        if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
            return [];

        Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(includePatterns);

        if (excludePatterns != null)
            matcher.AddExcludePatterns(excludePatterns);

        DirectoryInfo directoryInfo = new(baseDirectory);
        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));

        if (!result.HasMatches)
            return [];

        return result.Files.Select(file => new FileInfo(Path.Combine(baseDirectory, file.Path)));
    }

    public static FileInfo? GetFileInfo(this string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        string requestedFullPath;

        try
        {
            requestedFullPath = Path.GetFullPath(filePath);
        }
        catch (Exception)
        {
            return null;
        }

        FileInfo fileInfo = new(requestedFullPath);

        return fileInfo.Exists ? fileInfo : null;
    }

    public static string GetWebFriendlyPath(this string filePath) => filePath.Replace('\\', '/');

    public static bool IsExcludedByPatterns(this string filePath, IEnumerable<string> patterns)
    {
        Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(patterns);

        return matcher.Match(filePath).HasMatches;
    }
}