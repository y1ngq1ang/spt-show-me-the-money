using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Patches;

public class EditBuildScreenShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(typeof(EditBuildScreen), x => x.Name == nameof(EditBuildScreen.Show));

    [PatchPostfix]
#pragma warning disable CA1707 // Identifiers should not contain underscores

    public static void PatchPostfix(EditBuildScreen __instance)
#pragma warning restore CA1707 // Identifiers should not contain underscores

    {
        Diagnostics.ReportOnce(nameof(EditBuildScreenShowPatch), "EditBuildScreen.Show postfix fired");
        PluginContextHolder.SetDisableTemporary(true);
    }
}