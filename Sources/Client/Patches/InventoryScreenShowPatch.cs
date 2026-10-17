using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPTushonka.Reflection.Il2Cpp;
using SPTushonka.Reflection.Patching;
using SwiftXP.SPT.Common.EFT;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Services;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Patches;

/// <summary>
/// Marks the out-of-raid inventory screen as open, which is the only place quick-sell is allowed.
/// </summary>
public class InventoryScreenShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(typeof(InventoryScreen), x => x.Name == nameof(InventoryScreen.Show));

    [PatchPostfix]
    public static void PatchPostfix()
    {
        if (EFTHelper.IsInRaid)
            return;

        PluginContextHolder.SetIsInInventoryScreen(true);

        // Belt and braces for quick-sell: it needs the trader supply data at click time and cannot
        // wait for it. Normally MenuReadyPatch already warmed it; this covers the case where that
        // hook did not bind.
        MainThread.Post(TraderPriceService.PrefetchSupplyData);
    }
}
