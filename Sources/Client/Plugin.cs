using BepInEx;
using BepInEx.Unity.IL2CPP;
using SwiftXP.SPT.ShowMeTheMoney.Client.Configuration;
using SwiftXP.SPT.Common.Loggers;
using SPTushonka.Reflection.Patching;
using SwiftXP.SPT.ShowMeTheMoney.Client.Services;
using System.Threading;
using System;
using SwiftXP.SPT.ShowMeTheMoney.Client.Patches;
using SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client;

[BepInPlugin("com.dark.spt.showmethemoney", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.sptushonka.custom", "5.0.0")]
[BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("EscapeFromTarkov.exe")]
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
// BepInEx drives plugin lifetime through Load()/Unload(), and Unload() below disposes both token
// sources. Nothing would ever call Dispose() on the plugin itself, so implementing IDisposable
// would only add a second, unreachable disposal path.
public class Plugin : BasePlugin
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{

    public static void EnableTooltipUpdatePatch()
    {
        s_tooltipUpdatePatch!.Enable();
    }

    public static void DisableTooltipUpdatePatch()
    {
        s_tooltipUpdatePatch!.Disable();
    }

    /// <summary>
    /// The live plugin instance. Price Color Sync needs it to reach the price updater.
    /// </summary>
    internal static Plugin? Instance { get; private set; }

    public override void Load()
    {
        // Todo: Check if mod is running under fika headless.

        Instance = this;

        SimpleSptLogger simpleSptLogger = new(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION);
        PluginConfiguration pluginConfiguration = new(Config, OnForceUpdate);

        PluginContextHolder.SetContextInstances(simpleSptLogger, pluginConfiguration);

        _fleaPriceUpdaterCancellationTokenSource = new CancellationTokenSource();
        _partialRagfairConfigServiceCancellationTokenSource = new CancellationTokenSource();

        _ = new PartialRagfairConfigService(simpleSptLogger)
            .GetPartialRagfairConfigAsync(_partialRagfairConfigServiceCancellationTokenSource.Token);

        _fleaPriceUpdaterService = new FleaPriceUpdaterService(simpleSptLogger);
        _ = _fleaPriceUpdaterService.ContinuouslyUpdateFleaPricesAsync(_fleaPriceUpdaterCancellationTokenSource.Token);

        EnablePatches();
    }

    private static void EnablePatches()
    {
        // One patch that cannot bind (for example because a game update moved a method) must not
        // take the whole plugin down with it: ModulePatch.Enable() throws, and nothing else would
        // be applied.
        TryEnable(new MenuReadyPatch());

        TryEnable(new EditBuildScreenShowPatch());
        TryEnable(new EditBuildScreenClosePatch());

        TryEnable(new GridItemOnPointerEnterPatch());
        TryEnable(new GridItemOnPointerExitPatch());

        TryEnable(new InventoryScreenShowPatch());
        TryEnable(new InventoryScreenClosePatch());

        TryEnable(new SimpleTooltipShowPatch());

        // Quick-sell (integrated from spt-show-me-the-money-quick-sell).
        TryEnable(new GridItemViewOnClickPatch());

        s_tooltipUpdatePatch = new TooltipUpdatePatch();

        if (PluginContextHolder.Current.Configuration?.FleaTaxToggleMode.IsEnabled() ?? false)
            EnableTooltipUpdatePatch();

        Diagnostics.Report("all patches enabled");

        // Price Color Sync installs its own Harmony patches; it is not a ModulePatch.
        PriceColorSyncService.Initialize(PluginContextHolder.Current.Configuration);
    }

    private static void TryEnable(ModulePatch patch)
    {
        try
        {
            patch.Enable();
        }
        catch (Exception exception)
        {
            PluginContextHolder.Current?.SptLogger?
                .LogError($"Could not enable patch {patch.GetType().Name}: {exception.Message}");
        }
    }

    private void OnForceUpdate()
    {
        if (_fleaPriceUpdaterService != null)
            _fleaPriceUpdaterService.ForceUpdate();
    }

    internal void ForceFleaPriceUpdate()
    {
        _fleaPriceUpdaterService?.ForceUpdate();
    }

    public override bool Unload()
    {
        PriceColorSyncService.Shutdown();
        Instance = null;

        if (_fleaPriceUpdaterCancellationTokenSource != null)
        {
            _fleaPriceUpdaterCancellationTokenSource.Cancel();
            _fleaPriceUpdaterCancellationTokenSource.Dispose();
            _fleaPriceUpdaterCancellationTokenSource = null;
        }

        if (_partialRagfairConfigServiceCancellationTokenSource != null)
        {
            _partialRagfairConfigServiceCancellationTokenSource.Cancel();
            _partialRagfairConfigServiceCancellationTokenSource.Dispose();
            _partialRagfairConfigServiceCancellationTokenSource = null;
        }

        return true;
    }

    private CancellationTokenSource? _fleaPriceUpdaterCancellationTokenSource;

    private CancellationTokenSource? _partialRagfairConfigServiceCancellationTokenSource;

    private FleaPriceUpdaterService? _fleaPriceUpdaterService;

    private static ModulePatch? s_tooltipUpdatePatch;
}
