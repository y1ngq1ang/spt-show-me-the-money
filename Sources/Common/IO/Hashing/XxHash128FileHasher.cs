using System.IO.Hashing;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;

namespace SwiftXP.SPT.Common.IO.Hashing;

#if NET9_0_OR_GREATER
[SPTarkov.DI.Annotations.Injectable(SPTarkov.DI.Annotations.InjectionType.Singleton)]
#endif
public class XxHash128FileHasher : IXxHash128FileHasher
{
    private const int BufferSize = 1024 * 1024;

    public async Task<string?> GetFileHashAsync(FileInfo fileToHash, CancellationToken cancellationToken = default)
    {
        if (!fileToHash.Exists)
            return null;

        string fileHash = await GetXx128FileHashAsync(fileToHash, cancellationToken);

        return fileHash;
    }

    public async Task<Dictionary<string, string>> GetFileHashesAsync(IEnumerable<FileInfo> filesToHash, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (FileInfo fileInfo in filesToHash)
        {
            if (!fileInfo.Exists)
                continue;

            string fileHash = await GetXx128FileHashAsync(fileInfo, cancellationToken);
            result.Add(fileInfo.FullName, fileHash);
        }

        return result;
    }

    private async static Task<string> GetXx128FileHashAsync(FileInfo fileInfo, CancellationToken cancellationToken)
    {
        XxHash128 xxHash128 = new();
        using FileStream fileStream = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
        await xxHash128.AppendAsync(fileStream, cancellationToken);

        byte[] hashBytes = xxHash128.GetCurrentHash();
#if NET5_0_OR_GREATER
        return Convert.ToHexStringLower(hashBytes);
#else
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
#endif
    }
}