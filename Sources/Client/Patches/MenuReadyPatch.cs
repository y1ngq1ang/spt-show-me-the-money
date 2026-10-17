using System;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.UI.Screens;
using HarmonyLib;
using SPTushonka.Reflection.Il2Cpp;
using SPTushonka.Reflection.Patching;
using SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;
using SwiftXP.SPT.ShowMeTheMoney.Client.Services;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Patches;

/// <summary>
/// Warms the trader supply data once the main menu is active.
/// </summary>
/// <remarks>
/// <para>
/// The 4.x build did this from a postfix on the <c>Trader</c> constructor. That does not work on
/// SPT 5.0: the constructor runs while the profile is still being selected, and firing the supply
/// requests from there leaves the main menu stuck in its loading state — it never reaches
/// <c>InteractiveMenuReady</c>, and no exception is logged. The menu then renders empty.
/// </para>
/// <para>
/// Doing it lazily from the tooltip path instead would mean the first hover shows no trader price,
/// so the screen-change handler is used to find the point where the menu has become the active
/// screen, and the prefetch is given a moment to settle before running. Note that
/// <c>MainMenuShowOperation.Execute</c> is deliberately not hooked: it returns the IL2CPP-proxied
/// <c>Il2CppSystem.Threading.Tasks.Task&lt;T&gt;</c>, which Harmony cannot bind to a plain
/// <c>__result</c> parameter, and getting that wrong takes the whole plugin down.
/// </para>
/// </remarks>
public class MenuReadyPatch : ModulePatch
{
    private static readonly TimeSpan s_settleDelay = TimeSpan.FromSeconds(1.5);

    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(
            typeof(MainMenuShowOperation),
            x => x.Name == nameof(MainMenuShowOperation.ScreenChangeHandler));

    [PatchPostfix]
#pragma warning disable CA1707 // Identifiers should not contain underscores

    public static void PatchPostfix(EEftScreenType eftScreenType)
#pragma warning restore CA1707 // Identifiers should not contain underscores
    {
        if (eftScreenType != EEftScreenType.MainMenu)
            return;

        // Price Color Sync uses this as its "main menu is up" signal instead of the
        // MenuScreen.Show overload it targeted on SPT 4.1.x, which no longer exists.
        PriceColorSyncService.NotifyMenuShown();

        _ = SchedulePrefetchAsync();
    }

    private static async Task SchedulePrefetchAsync()
    {
        try
        {
            await Task.Delay(s_settleDelay).ConfigureAwait(false);

            MainThread.Post(TraderPriceService.PrefetchSupplyData);
        }
        catch (Exception exception)
        {
            // Nothing here may escape onto a worker thread unobserved.
            Diagnostics.Report($"menu-ready prefetch could not be scheduled: {exception.Message}");
        }
    }
}
