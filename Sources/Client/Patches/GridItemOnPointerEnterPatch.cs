using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using System.Reflection;
using UnityEngine.EventSystems;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Patches;

public class GridItemOnPointerEnterPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(typeof(GridItemView), x => x.Name == nameof(GridItemView.OnPointerEnter));

    [PatchPrefix]
    static void PatchPrefix(GridItemView __instance, PointerEventData eventData)
    {
        Diagnostics.ReportOnce(nameof(GridItemOnPointerEnterPatch), "GridItemView.OnPointerEnter prefix fired");
        PluginContextHolder.SetHoveredItem(__instance?.Item);
    }
}