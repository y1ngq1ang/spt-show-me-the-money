using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTushonka.Common.Http;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Services;

public class PartialRagfairConfigService(ISimpleSptLogger simpleSptLogger)
{
    public async Task GetPartialRagfairConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            string jsonResult = await RequestHandler.GetJsonAsync(Constants.RemotePathToGetPartialRagfairConfig);

            if (cancellationToken.IsCancellationRequested)
                return;

            PartialRagfairConfig? partialRagfairConfig = JsonSerializer.Deserialize<PartialRagfairConfig>(
                jsonResult, Constants.JsonOptions);

            if (cancellationToken.IsCancellationRequested)
                return;

            if (partialRagfairConfig != null)
                PartialRagfairConfigHolder.UpdateData(partialRagfairConfig);
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            simpleSptLogger.LogException(ex);
        }
    }
}
