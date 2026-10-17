using EFT.UI;
using SPTushonka.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
using SwiftXP.SPT.Common.EFT;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Patches;

public class InventoryScreenClosePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(typeof(InventoryScreen), x => x.Name == nameof(InventoryScreen.Close));

    // The prefix takes no __instance: it never used one, and the old declaration typed it as
    // Tooltip even though the patched method belongs to InventoryScreen, which Harmony cannot bind.
    [PatchPrefix]

    public static void PatchPrefix()
    {
        Diagnostics.ReportOnce(nameof(InventoryScreenClosePatch), "InventoryScreen.Close prefix fired");
        PluginContextHolder.SetHoveredItem(null);
        SimpleTooltipShowPatch.OnClose();
    }

    [PatchPostfix]
    public static void PatchPostfix()
    {
        if (!EFTHelper.IsInRaid)
            PluginContextHolder.SetIsInInventoryScreen(false);
    }
}
