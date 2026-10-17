using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SPTushonka.Common.Http;
using SPT.Common.Models;
using UnityEngine.Networking;

namespace SwiftXP.SPT.Common.Http;

public class SPTHttpClient : ISPTHttpClient
{
    public HttpClient HttpClient => RequestHandler.HttpClient.HttpClient;

    public async Task DownloadWithCancellationAsync(
        string path,
        string filePath,
        Action<DownloadProgress>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        Uri? baseUri;
        using (HttpRequestMessage dummyRequest = RequestHandler.HttpClient.CreateNewHttpRequest(HttpMethod.Get, ""))
            baseUri = dummyRequest.RequestUri;

        if (baseUri is null)
            throw new HttpRequestException("Unable to retrieve base URI.");

        Uri fullUri = new(baseUri, path);
        string fullUrl = fullUri.AbsoluteUri;

        string? directoryName = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);

        using UnityWebRequest request = UnityWebRequest.Get(fullUrl);
        request.downloadHandler = new DownloadHandlerFile(filePath)
        {
            removeFileOnAbort = true
        };

        request.certificateHandler = new FakeCertificateHandler();

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        DateTime startTime = DateTime.UtcNow;
        long totalBytes = 0L;

        while (!operation.isDone)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                request.Abort();
                cancellationToken.ThrowIfCancellationRequested();
            }

            DateTime utcNow = DateTime.UtcNow;
            long downloadedBytes = (long)request.downloadedBytes;

            if (totalBytes == 0L && request.GetResponseHeader("Content-Length") != null &&
                long.TryParse(request.GetResponseHeader("Content-Length"), out long result))
            {
                totalBytes = result;
            }

            double totalSeconds = (utcNow - startTime).TotalSeconds;
            double bytesPerSecond = (totalSeconds > 0.0) ? (downloadedBytes / totalSeconds) : 0.0;

            progressCallback?.Invoke(new DownloadProgress
            {
                DownloadSpeed = DownloadProgress.FormatDownloadSpeed(bytesPerSecond),
                FileSizeInfo = DownloadProgress.FormatFileSize(downloadedBytes) + " / " + DownloadProgress.FormatFileSize(totalBytes)
            });

            await Task.Delay(25, cancellationToken);
        }

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new HttpRequestException($"Download failed: {request.error}");
        }
    }
}