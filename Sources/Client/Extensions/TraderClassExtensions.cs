using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Trading;
using SPTushonka.Reflection.Il2Cpp;
using SwiftXP.SPT.Common.Sessions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Extensions;

public static class TraderClassExtensions
{
    /// <summary>
    /// The trader's cached supply data, or <see langword="null" /> when it has not been fetched yet.
    /// </summary>
    /// <remarks>
    /// The 4.x build reached the private <c>_supplyData</c> field through reflection. Il2CppInterop
    /// surfaces game fields as properties on the proxy type, so the property is used directly.
    /// </remarks>
    public static SupplyData? GetSupplyData(this Trader trader) => trader._supplyData;

    /// <summary>
    /// Fetches and caches the trader's supply data if the game has not done so yet.
    /// </summary>
    /// <remarks>
    /// Call this only once the menu is up. Triggering it while the profile is still being selected
    /// keeps the main menu from ever finishing its loading sequence — see <see cref="Patches.MenuReadyPatch" />.
    /// </remarks>
    public static async void UpdateSupplyData(this Trader trader)
    {
        if (trader._supplyData is not null || !TryBeginRequest(trader.Id))
            return;

        try
        {
            Diagnostics.Report($"supply data: requesting for trader {trader.Id}");

            Result<SupplyData> result = await SptSession.Session.GetSupplyData(trader.Id);

            Diagnostics.Report($"supply data: trader {trader.Id} returned (failed={result.Failed})");

            if (result.Failed)
            {
                PluginContextHolder.Current.SptLogger?
                    .LogError("Unable to update supply data for trader(s)! Plug-in will not work properly without that data");

                return;
            }

            SupplyData supplyData = result.Value;

            // The continuation resumes on a worker thread; the field belongs to an IL2CPP object,
            // so hand the write back to the Unity main thread.
            MainThread.Post(() => trader._supplyData = supplyData);
        }
        catch (Exception exception)
        {
            PluginContextHolder.Current.SptLogger?
                .LogException(exception);
        }
        finally
        {
            EndRequest(trader.Id);
        }
    }

    /// <summary>
    /// Guards against firing the same trader's request twice before the first one lands, since the
    /// pricing path can ask on every tooltip.
    /// </summary>
    private static bool TryBeginRequest(string traderId)
    {
        lock (s_inFlight)
            return s_inFlight.Add(traderId);
    }

    private static void EndRequest(string traderId)
    {
        lock (s_inFlight)
            s_inFlight.Remove(traderId);
    }

    private static readonly HashSet<string> s_inFlight = [];
}
