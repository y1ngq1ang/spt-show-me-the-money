using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTushonka.Common.Http;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.Common.EFT;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Services;

public class FleaPriceUpdaterService(ISimpleSptLogger simpleSptLogger)
{
    public async Task ContinuouslyUpdateFleaPricesAsync(CancellationToken cancellationToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(30);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_forceUpdate ||
                    ((!EFTHelper.IsInRaid || PluginContextHolder.Current!.Configuration!.UpdateDuringRaid.IsEnabled())
                        && (FleaPriceDataHolder.Current == null
                            || (DateTimeOffset.UtcNow - FleaPriceDataHolder.Current.Timestamp).TotalMinutes >= PluginContextHolder.Current!.Configuration!.UpdateInterval.GetValue())))
                {
                    _forceUpdate = false;

                    // Must go through SPTushonka's RequestHandler: it unwraps the framing SPT 5.0
                    // applies to every non-launcher route. Calling the underlying HttpClient directly
                    // hands back a still-shuffled payload that no JSON reader can make sense of.
                    byte[] payload = await RequestHandler.GetDataAsync(Constants.RemotePathToGetFleaPrices);

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    Dictionary<string, double>? fleaPrices = JsonSerializer.Deserialize<Dictionary<string, double>>(
                        payload, Constants.JsonOptions);

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (fleaPrices != null)
                        FleaPriceDataHolder.UpdateData(fleaPrices);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                simpleSptLogger.LogException(ex);
            }
            finally
            {
                await Task.Delay(interval, cancellationToken);
            }
        }
    }

    public void ForceUpdate()
    {
        _forceUpdate = true;
    }

    private volatile bool _forceUpdate;
}
