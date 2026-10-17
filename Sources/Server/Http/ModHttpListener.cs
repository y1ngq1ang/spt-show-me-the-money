using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Servers.Http;
using SPTarkov.Server.Core.Utils;
using SwiftXP.SPT.ShowMeTheMoney.Server.Services;

namespace SwiftXP.SPT.ShowMeTheMoney.Server.Http;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.Preload)]
public class ModHttpListener(
    ISptLogger<ModHttpListener> sptLogger,
    RequestEncryptionUtil requestEncryptionUtil,
    FleaPricesService fleaPricesService,
    RagfairConfigService ragfairConfigService) : IHttpListener
{
    private static readonly PathString s_pathGetFleaPrices = new($"{Constants.RoutePrefix}{Constants.RouteGetFleaPrices}");
    private static readonly PathString s_pathGetPartialRagfairConfig = new($"{Constants.RoutePrefix}{Constants.RouteGetPartialRagfairConfig}");

    public bool CanHandle(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(Constants.RoutePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(MongoId sessionId, HttpContext context, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            PathString path = context.Request.Path;

            if (path.Equals(s_pathGetFleaPrices, StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetFleaPricesAsync(context, cancellationToken);
            }
            else if (path.Equals(s_pathGetPartialRagfairConfig, StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetPartialRagfairConfigAsync(context, cancellationToken);
            }
            else
            {
                await HandleUnknownRouteAsync(context, path.Value ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            sptLogger.Error($"{Constants.LoggerPrefix}[ERROR] Error handling request: {ex.Message}");

            context.Response.StatusCode = 500;
        }
    }

    private async Task HandleGetFleaPricesAsync(HttpContext context, System.Threading.CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, double> result = fleaPricesService.Get();

        await WriteShuffledJsonAsync(context, result, cancellationToken);
    }

    private async Task HandleGetPartialRagfairConfigAsync(HttpContext context, System.Threading.CancellationToken cancellationToken)
    {
        Data.PartialRagfairConfig result = ragfairConfigService.Get();

        await WriteShuffledJsonAsync(context, result, cancellationToken);
    }

    /// <summary>
    /// Writes a JSON body in the framed, shuffled shape the 5.0 client expects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SPT 5.0 shuffles request and response bodies for every route except <c>/launcher</c>,
    /// <c>/client/metadata</c>, <c>/v2/shop</c>, <c>/files</c> and <c>/singleplayer</c>, and the client
    /// unpacks unconditionally on its side. A hand-rolled <see cref="IHttpListener" /> sits outside
    /// that pipeline, so serving plain JSON makes the client try to unshuffle a frame that was never
    /// shuffled and fail with "deshuffle produced a bad length".
    /// </para>
    /// <para>
    /// This mirrors <c>SptHttpListener.WriteShuffledAsync</c>: a 4-byte little-endian payload length,
    /// the payload, then padding. <c>ShuffleInPlace</c> writes the length header, fills the padding by
    /// repeating the payload and shuffles the whole frame, so only the payload has to be placed.
    /// Leaving the body uncompressed is fine — the client's zlib check simply does not trigger.
    /// </para>
    /// </remarks>
    private async Task WriteShuffledJsonAsync(HttpContext context, object? value, System.Threading.CancellationToken cancellationToken)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web);
        byte[] frame = new byte[4 + payload.Length + requestEncryptionUtil.NextPadding()];

        payload.CopyTo(frame, 4);
        requestEncryptionUtil.ShuffleInPlace(frame, payload.Length);

        context.Response.ContentType = "application/json";
        await context.Response.Body.WriteAsync(frame, cancellationToken);
    }

    /// <remarks>
    /// A bodyless response is safe here: the client throws on the non-success status code before it
    /// ever tries to unpack the body.
    /// </remarks>
    private Task HandleUnknownRouteAsync(HttpContext context, string requestPath)
    {
        sptLogger.Warning($"{Constants.LoggerPrefix}[WARNING] Unknown route: {requestPath}");

        context.Response.StatusCode = 404;

        return Task.CompletedTask;
    }
}
