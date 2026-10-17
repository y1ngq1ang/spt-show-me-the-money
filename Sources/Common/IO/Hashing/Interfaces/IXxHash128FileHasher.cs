using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftXP.SPT.Common.IO.Hashing;

public interface IXxHash128FileHasher
{
    Task<string?> GetFileHashAsync(FileInfo fileToHash, CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>> GetFileHashesAsync(IEnumerable<FileInfo> filesToHash, CancellationToken cancellationToken = default);
}