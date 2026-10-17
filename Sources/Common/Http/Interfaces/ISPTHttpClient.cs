using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SPTushonka.Common.Http;

namespace SwiftXP.SPT.Common.Http;

public interface ISPTHttpClient
{
    HttpClient HttpClient { get; }

    Task DownloadWithCancellationAsync(
        string path,
        string filePath,
        Action<DownloadProgress>? progressCallback,
        CancellationToken cancellationToken);
}