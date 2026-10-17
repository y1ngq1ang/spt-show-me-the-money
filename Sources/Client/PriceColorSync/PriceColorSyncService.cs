using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SwiftXP.SPT.Common.EFT;
using SwiftXP.SPT.Common.Sessions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Configuration;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Patches;
using SwiftXP.SPT.ShowMeTheMoney.Client.Services;
using SwiftXP.SPT.ShowMeTheMoney.Client.Utilities;
using UnityEngine;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;

/// <summary>
/// Keeps inventory background colours and the hover tooltip on the same item valuation, and owns the
/// tooltip price block that Show Me The Money would otherwise render itself.
/// </summary>
/// <remarks>
/// Integrated from GPTSol6-1/PriceColorSync 1.1.4, which was written as a separate plugin against
/// SMTM 3.0.2 on SPT 4.1.6. Two things had to change beyond the port: the members it used to reach
/// by reflection are now called directly (same assembly), and the main-menu signal comes from
/// <see cref="MenuReadyPatch" /> instead of a <c>MenuScreen.Show</c> overload that no longer exists
/// in SPT 5.0.
/// </remarks>
internal static class PriceColorSyncService
{
    private static readonly MenuRefreshPolicy s_menuRefresh = new();
    private static readonly Stopwatch s_backgroundTimer = new();
    private static readonly Dictionary<Item, HashSet<ItemView>> s_itemViews = new();
    private static readonly ConditionalWeakTable<ItemView, ViewState> s_viewStates = new();
    private static readonly ItemPreparationQueue<Item> s_pendingBackgrounds = new();
    private static readonly ConditionalWeakTable<Item, Quote> s_quotes = new();
    private static readonly string[] s_defaultBackgrounds = ["#404040", "#a3a3a3", "#0c3b08", "#08083b", "#590b5e", "#5e470b"];
    private static readonly Color[] s_backgrounds = new Color[6];
    private static readonly double[] s_priceBoundaries = new double[5];
    private static readonly double[] s_penBoundaries = new double[5];

    private static Harmony? s_harmony;
    private static PluginConfiguration? s_config;
    private static Color s_fleaBannedBackground;
    private static volatile bool s_captureItems;
    private static bool s_initialized;
    private static bool s_fleaForFir;
    private static bool s_fleaForNonFir;
    private static bool s_traderForBanned;
    private static bool s_showAmmoStats;
    private static int s_maximumBackgroundItems;
    private static float s_backgroundBudgetMs;
    private static int s_valuationEpoch;
    private static long s_preparedCount;
    private static long s_coldViewCount;
    private static float s_nextWarning;
    private static bool s_awaitingMarketRefresh;
    private static object? s_requestedMarketSnapshot;
    private static object? s_lastMarketSnapshot;

    [ThreadStatic]
    private static int t_pricingDepth;

    public static void Initialize(PluginConfiguration? configuration)
    {
        if (s_initialized)
            return;

        try
        {
            s_config = configuration;
            if (s_config == null)
                throw new InvalidOperationException("Plugin configuration is not available yet.");

            if (!s_config.PriceColorSyncEnabled.Value)
            {
                Diagnostics.Report("Price Color Sync disabled by configuration.");
                return;
            }

            s_fleaForFir = s_config.PriceColorSyncFleaForFir.Value;
            s_fleaForNonFir = s_config.PriceColorSyncFleaForNonFir.Value;
            s_traderForBanned = s_config.PriceColorSyncTraderForBanned.Value;
            s_showAmmoStats = s_config.PriceColorSyncShowAmmoStats.Value;
            s_maximumBackgroundItems = s_config.PriceColorSyncMaxItemsPerFrame.Value;
            s_backgroundBudgetMs = s_config.PriceColorSyncBudgetMilliseconds.Value;

            string[] tiers =
            [
                s_config.PriceColorSyncTier0.Value,
                s_config.PriceColorSyncTier1.Value,
                s_config.PriceColorSyncTier2.Value,
                s_config.PriceColorSyncTier3.Value,
                s_config.PriceColorSyncTier4.Value,
                s_config.PriceColorSyncTier5.Value,
            ];

            for (int i = 0; i < tiers.Length; i++)
            {
                if (!ColorUtility.TryParseHtmlString(tiers[i], out s_backgrounds[i]))
                {
                    if (!ColorUtility.TryParseHtmlString(s_defaultBackgrounds[i], out s_backgrounds[i]))
                        s_backgrounds[i] = Color.white;

                    Diagnostics.Report($"Price Color Sync: invalid background colour '{tiers[i]}', using {s_defaultBackgrounds[i]}.");
                }
            }

            if (!ColorUtility.TryParseHtmlString(s_config.PriceColorSyncFleaBannedBackground.Value, out s_fleaBannedBackground))
            {
                if (!ColorUtility.TryParseHtmlString("#660415", out s_fleaBannedBackground))
                    s_fleaBannedBackground = Color.white;

                Diagnostics.Report("Price Color Sync: invalid flea-banned colour, using #660415.");
            }

            s_harmony = new Harmony("com.dark.spt.showmethemoney.pricecolorsync");

            TryPatch(typeof(TarkovTickPatch));
            TryPatch(typeof(ViewInitPatch));
            TryPatch(typeof(InventorySimulationPatch));
            TryPatch(typeof(ItemCreatedPatch));
            TryPatch(typeof(ItemRefreshedPatch));
            TryPatch(typeof(ItemMovedPatch));
            TryPatch(typeof(ItemFirPatch));
            TryPatch(typeof(RaidPreparedPatch));
            TryPatch(typeof(ViewColorPatch));
            TryPatch(typeof(ViewKillPatch));
            TryPatch(typeof(CaseTooltipRequirementsPatch));
            TryPatch(typeof(TooltipPricePatch));

            s_captureItems = true;
            s_initialized = true;

            Diagnostics.Report(
                $"Price Color Sync enabled: FIR flea={s_fleaForFir}; non-FIR flea={s_fleaForNonFir}; "
                + $"flea-banned trader={s_traderForBanned}; ammo stats={s_showAmmoStats}; "
                + $"{s_maximumBackgroundItems} items/frame, {s_backgroundBudgetMs} ms budget.");
        }
        catch (Exception exception)
        {
            s_captureItems = false;
            s_initialized = false;
            s_harmony?.UnpatchSelf();
            s_harmony = null;
            Diagnostics.Report($"Price Color Sync disabled: {exception}");
        }
    }

    public static void Shutdown()
    {
        s_captureItems = false;
        s_initialized = false;
        s_harmony?.UnpatchSelf();
        s_harmony = null;
        s_itemViews.Clear();
        s_pendingBackgrounds.Clear();

        if (s_preparedCount != 0 || s_coldViewCount != 0)
        {
            Diagnostics.Report(
                $"Price Color Sync preparation totals: {s_preparedCount} background quotes; "
                + $"{s_coldViewCount} cold/stale first-display quotes.");
        }
    }

    /// <summary>
    /// Called by <see cref="MenuReadyPatch" /> when the main menu becomes the active screen.
    /// </summary>
    internal static void NotifyMenuShown()
    {
        s_menuRefresh.MenuShown();
    }

    private static void TryPatch(Type patchType)
    {
        try
        {
            s_harmony!.CreateClassProcessor(patchType).Patch();
        }
        catch (Exception exception)
        {
            Diagnostics.Report($"Price Color Sync: patch {patchType.Name} could not be applied: {exception.Message}");
        }
    }

    private static bool Enabled => s_initialized && s_config != null;

    private static void Tick()
    {
        if (!Enabled)
            return;

        try
        {
            UpdateMenuRefresh();

            object? snapshot = FleaPriceDataHolder.Current;

            if (s_awaitingMarketRefresh && snapshot != null && !ReferenceEquals(snapshot, s_requestedMarketSnapshot))
            {
                s_awaitingMarketRefresh = false;
                Diagnostics.Report("Price Color Sync: replacement flea price snapshot received.");
            }

            if (!ReferenceEquals(snapshot, s_lastMarketSnapshot))
            {
                s_lastMarketSnapshot = snapshot;
                s_valuationEpoch++;
                s_pendingBackgrounds.ScheduleAll();
            }

            if (!PricingReady || s_pendingBackgrounds.Count == 0)
                return;

            s_backgroundTimer.Restart();

            try
            {
                s_pendingBackgrounds.Process(s_maximumBackgroundItems, ProcessBackground, WithinBackgroundBudget);
            }
            finally
            {
                s_backgroundTimer.Stop();
            }
        }
        catch (Exception exception)
        {
            Warn(exception);
        }
    }

    private static bool WithinBackgroundBudget()
    {
        return s_backgroundTimer.Elapsed.TotalMilliseconds < s_backgroundBudgetMs;
    }

    private static void UpdateMenuRefresh()
    {
        bool inRaid = EFTHelper.IsInRaid;
        s_menuRefresh.ObserveRaid(inRaid);

        if (!TryGetSession(out IEftSession? session))
            return;

        string? reason = s_menuRefresh.PendingReason(inRaid, session.Profile != null);

        if (reason == null)
            return;

        try
        {
            Plugin? plugin = Plugin.Instance;
            if (plugin == null)
                return;

            s_requestedMarketSnapshot = FleaPriceDataHolder.Current;
            plugin.ForceFleaPriceUpdate();
            s_valuationEpoch++;
            s_pendingBackgrounds.ScheduleAll();
            s_awaitingMarketRefresh = true;
            s_menuRefresh.Requested();
            Diagnostics.Report("Price Color Sync: automatic flea price refresh requested (" + reason + ").");
        }
        catch (Exception exception)
        {
            Diagnostics.Report("Price Color Sync: automatic refresh request failed: " + exception.Message);
        }
    }

    /// <remarks>
    /// <c>SptSession.Session</c> throws while the client application is not up yet, and this runs
    /// every frame, so the failure has to be swallowed here rather than logged.
    /// </remarks>
    private static bool TryGetSession([NotNullWhen(true)] out IEftSession? session)
    {
        try
        {
            session = SptSession.Session;

            return session != null;
        }
        catch (Exception)
        {
            session = null;

            return false;
        }
    }

    private static bool PricingReady
    {
        get
        {
            if (s_config == null || !s_config.EnablePlugin.Value)
                return false;

            if (!TryGetSession(out IEftSession? session))
                return false;

            try
            {
                return session!.Profile != null && session.Traders != null && session.Traders.Any();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal static void Warn(Exception exception)
    {
        if (Time.unscaledTime < s_nextWarning)
            return;

        s_nextWarning = Time.unscaledTime + 30f;
        Diagnostics.Report("Price Color Sync could not calculate value: " + exception.Message);
    }

    private sealed class Quote
    {
        public TradeItem? Trade;
        public TradePrice? Price;
        public double? Compare;
        public float Expires;
        public bool Fir;
        public bool UsesFlea;
        public bool DependsOnFlea;
        public bool FleaEligible;
        public object? MarketSnapshot;
        public bool Dirty;
        public int Generation;
        public int Epoch;
        public long Revision;
    }

    private sealed class ViewState
    {
        public Item? Item;
        public int PaintedGeneration = -1;
    }

    private static bool IsAmmo(Item item) => item is Ammo or AmmoBox;

    private static bool IsStorageCase(Item item) => item is SimpleContainer;

    private static Item CreateCasePricingShell(Item original)
    {
        // GetDiff contains this item's own components/state, not its contained grids.
        // CreateItem constructs a detached empty instance; CloneItem would recursively
        // copy the inventory tree first. Never clear or modify the live case.
        ItemFactory factory = Singleton<ItemFactory>.Instance;
        Item shell = factory.CreateItem(MongoID.Generate(true), original.TemplateId, factory.GetDiff(original));

        shell.StackObjectsCount = original.StackObjectsCount;

        return shell;
    }

    private static ViewState RegisterView(ItemView view)
    {
        ViewState state = s_viewStates.GetOrCreateValue(view);

        if (ReferenceEquals(state.Item, view.Item))
            return state;

        UnregisterView(view);
        state.Item = view.Item;
        state.PaintedGeneration = -1;

        if (state.Item != null)
        {
            if (!s_itemViews.TryGetValue(state.Item, out HashSet<ItemView>? views))
                s_itemViews[state.Item] = views = [];

            views.Add(view);
        }

        return state;
    }

    private static void UnregisterView(ItemView view)
    {
        if (!s_viewStates.TryGetValue(view, out ViewState? state) || state.Item == null)
            return;

        if (s_itemViews.TryGetValue(state.Item, out HashSet<ItemView>? views))
        {
            views.Remove(view);

            if (views.Count == 0)
                s_itemViews.Remove(state.Item);
        }

        state.Item = null;
        state.PaintedGeneration = -1;
    }

    private static bool NeedsQuote(Item item, Quote quote, bool interactive)
    {
        return quote.Trade == null || quote.Dirty || quote.Fir != item.MarkedAsSpawnedInSession
            || quote.Epoch != s_valuationEpoch || quote.Revision != s_pendingBackgrounds.Revision(item)
            || (quote.DependsOnFlea && !ReferenceEquals(quote.MarketSnapshot, FleaPriceDataHolder.Current))
            || (interactive && quote.Expires <= Time.unscaledTime);
    }

    private static void ProcessBackground(Item item)
    {
        // Skip temporary trader stock and detached clones. Real instances become
        // owned after deserialization; a view can still quote an unowned item later.
        if (item is Stash or InventoryEquipment or LootContainer
            or MobContainer or Pockets or HideoutAreaContainer or StationaryContainer)
        {
            return;
        }

        if (item.Owner == null || item.Owner.OwnerType == EOwnerType.Trader)
            return;

        if (IsAmmo(item) && s_config!.UseCaliberPenetrationPower.Value)
            return;

        Quote previous = s_quotes.GetOrCreateValue(item);
        bool needed = NeedsQuote(item, previous, false);
        Quote quote = GetQuote(item, false);

        if (needed)
            s_preparedCount++;

        RefreshItemViews(item, quote);
    }

    private static void ItemChanged(Item? item)
    {
        if (t_pricingDepth != 0 || InventorySimulationScope.Active || !s_captureItems || item == null)
            return;

        s_pendingBackgrounds.Schedule(item, true);

        // Refresh weapon/armor parents when parts move or change condition. Storage
        // cases intentionally retain their shell-only quote when contents change.
        // onlyMerged=true stops at inventory grids, avoiding pricing the entire
        // stash/backpack when one contained item changes.
        try
        {
            foreach (Item parent in item.GetAllParentItems(true))
            {
                if (!IsStorageCase(parent))
                    s_pendingBackgrounds.Schedule(parent, true);
            }
        }
        catch (Exception exception)
        {
            // A malformed/transitional address must not interrupt the game's event.
            // Hooks may run off-thread, so avoid Unity Time/Component APIs here.
            Diagnostics.Report("Price Color Sync: item preparation could not invalidate parent quotes: " + exception.GetType().Name);
        }
    }

    private static void PrepareFirstDisplay(ItemView view)
    {
        if (InventorySimulationScope.Active || view?.Item == null || !view.Examined || !PricingReady)
            return;

        if (IsAmmo(view.Item) && s_config!.UseCaliberPenetrationPower.Value)
            return;

        Quote quote = s_quotes.GetOrCreateValue(view.Item);

        if (!NeedsQuote(view.Item, quote, false))
            return;

        // Rare cold/changed items must be priced before UpdateStaticInfo calls
        // UpdateColor. Never substitute a template average for the actual item.
        s_coldViewCount++;
        GetQuote(view.Item, false);
    }

    private static void RefreshItemViews(Item item, Quote quote)
    {
        if (!s_itemViews.TryGetValue(item, out HashSet<ItemView>? views))
            return;

        foreach (ItemView view in views)
        {
            if (view == null || !view.isActiveAndEnabled)
                continue;

            ViewState state = s_viewStates.GetOrCreateValue(view);

            if (state.PaintedGeneration != quote.Generation)
                view.UpdateColor();
        }
    }

    private static Quote GetQuote(Item item, bool interactive)
    {
        Quote result = s_quotes.GetOrCreateValue(item);
        object? snapshot = FleaPriceDataHolder.Current;

        if (!NeedsQuote(item, result, interactive))
            return result;

        result.Trade = null;
        result.Price = null;
        result.Compare = null;
        result.Fir = item.MarkedAsSpawnedInSession;
        result.MarketSnapshot = snapshot;
        result.Expires = Time.unscaledTime + 1f;
        result.Dirty = false;
        result.Generation++;
        result.Epoch = s_valuationEpoch;
        result.Revision = s_pendingBackgrounds.Revision(item);

        if (s_config == null)
            return result;

        t_pricingDepth++;

        try
        {
            Item pricingItem = IsStorageCase(item) ? CreateCasePricingShell(item) : item;
            result.FleaEligible = pricingItem.CanSellOnRagfair;
            result.UsesFlea = TierPolicy.PriceSource(result.Fir, result.FleaEligible,
                s_fleaForFir, s_fleaForNonFir, s_traderForBanned) == "Flea";

            // A trader-winning FIR quote must still be reconsidered after market refresh.
            result.DependsOnFlea = result.UsesFlea;
            result.Trade = new TradeItem(pricingItem, null, null);

            if (result.UsesFlea)
            {
                FleaPriceUtility.GetFleaPrice(result.Trade, s_config.IncludeFleaTax, out _);

                // Optional legacy estimate mode affects presentation only.
                if (result.Trade.FleaPrice == null && !result.FleaEligible)
                    EstimateBannedFleaItem(result.Trade);

                result.Price = result.Trade.FleaPrice;

                // Cache the competing offer for both FIR and optional non-FIR
                // flea tooltips. Only FIR mode selects the higher valuation.
                TraderPriceService.Instance.GetBestTraderPrice(result.Trade);

                if (result.Fir
                    && TierPolicy.PreferTrader(ComparePrice(result.Price), ComparePrice(result.Trade.TraderPrice)))
                {
                    result.Price = result.Trade.TraderPrice;
                    result.UsesFlea = false;
                }
            }
            else
            {
                TraderPriceService.Instance.GetBestTraderPrice(result.Trade);
                result.Price = result.Trade.TraderPrice;
            }

            result.Compare = ComparePrice(result.Price);
        }
        catch (Exception exception)
        {
            result.Dirty = true;
            Warn(exception);
        }
        finally
        {
            t_pricingDepth--;
        }

        return result;
    }

    private static double? ComparePrice(TradePrice? price)
    {
        double? value = price?.GetComparePriceInRouble();

        return TierPolicy.IsValidPrice(value) ? value : null;
    }

    private static void EstimateBannedFleaItem(TradeItem trade)
    {
        FleaPriceData? market = FleaPriceDataHolder.Current;

        if (market == null || !market.Prices.TryGetValue(trade.Item.TemplateId, out double price))
            return;

        if (trade.Item is Weapon weapon)
        {
            price = FleaPriceUtility.GetWeaponPrice(weapon, price);
        }
        else
        {
            PartialRagfairConfig? partial = PartialRagfairConfigHolder.Current;

            if (partial?.ItemPriceMultiplier != null && partial.ItemPriceMultiplier.TryGetValue(trade.Item.TemplateId, out double multiplier))
                price *= multiplier;
        }

        double quality = ItemQualityUtility.GetItemQualityModifier(trade.Item);
        price = SellChangeUtility.GetPriceForDesiredSellChange(price * quality, quality, -1);

        if (price > 0)
            FleaPriceUtility.SetFleaPriceOfTradeItem(trade, price, s_config!.IncludeFleaTax);
    }

    private static int GetTier(Item item, Quote? quote)
    {
        PluginConfiguration? config = s_config;

        if (config == null)
            return -1;

        Ammo? ammo = item as Ammo;

        if (item is AmmoBox box)
            ammo = box.Cartridges.Items.OfType<Ammo>().FirstOrDefault();

        if (ammo != null && config.UseCaliberPenetrationPower.Value)
        {
            s_penBoundaries[0] = (double)config.PoorPenetrationValue.Value;
            s_penBoundaries[1] = (double)config.CommonPenetrationValue.Value;
            s_penBoundaries[2] = (double)config.UncommonPenetrationValue.Value;
            s_penBoundaries[3] = (double)config.RarePenetrationValue.Value;
            s_penBoundaries[4] = (double)config.EpicPenetrationValue.Value;

            return TierPolicy.Tier(ammo.PenetrationPower, s_penBoundaries);
        }

        if ((item is AmmoBox && ammo == null && config.UseCaliberPenetrationPower.Value)
            || quote == null || !quote.Compare.HasValue)
        {
            return -1;
        }

        s_priceBoundaries[0] = (double)config.PoorValue.Value;
        s_priceBoundaries[1] = (double)config.CommonValue.Value;
        s_priceBoundaries[2] = (double)config.UncommonValue.Value;
        s_priceBoundaries[3] = (double)config.RareValue.Value;
        s_priceBoundaries[4] = (double)config.EpicValue.Value;

        return TierPolicy.Tier(quote.Compare.Value, s_priceBoundaries);
    }

    internal static void ApplyBackground(ItemView? view)
    {
        if (view?.Item == null || !view.Examined || !Enabled || !s_config!.EnablePlugin.Value)
            return;

        try
        {
            ViewState state = RegisterView(view);
            Quote quote = s_quotes.GetOrCreateValue(view.Item);

            // UI repaint events never perform price calculations. Ammo backgrounds
            // need only penetration; other missing/stale quotes go to the limited queue.
            bool pending = (!IsAmmo(view.Item) || !s_config.UseCaliberPenetrationPower.Value)
                && NeedsQuote(view.Item, quote, false);

            if (pending)
                s_pendingBackgrounds.Schedule(view.Item, false);

            int tier = GetTier(view.Item, quote);

            // The ban marker is template-based, so it works before quotes settle without
            // calculating prices on a repaint. Ammo keeps penetration or price tiers.
            Color color = !IsAmmo(view.Item) && !view.Item.Template.CanSellOnRagfair
                ? s_fleaBannedBackground
                : s_backgrounds[tier < 0 ? 0 : tier];

            color.a = view.BackgroundColor.a;

            if (!view.BackgroundColor.Equals(color))
                view.BackgroundColor = color;

            if (!view.OriginalBackgroundColor.Equals(color))
                view.OriginalBackgroundColor = color;

            if (view.Animator != null && !view.Animator._defaultBackgroundColor.Equals(color))
                view.Animator._defaultBackgroundColor = color;

            if (!pending)
                state.PaintedGeneration = quote.Generation;
        }
        catch (Exception exception)
        {
            Warn(exception);
        }
    }

    // ---------------------------------------------------------------------
    // Harmony patches
    // ---------------------------------------------------------------------

    [HarmonyPatch(typeof(TarkovApplication), "Update")]
    private static class TarkovTickPatch
    {
        private static void Postfix() => Tick();
    }

    [HarmonyPatch(typeof(ItemView), "UpdateStaticInfo")]
    private static class ViewInitPatch
    {
        private static void Prefix(ItemView __instance)
        {
            try
            {
                PrepareFirstDisplay(__instance);
            }
            catch (Exception exception)
            {
                Warn(exception);
            }
        }

        private static void Postfix(ItemView __instance)
        {
            RegisterView(__instance);
            __instance.UpdateColor();
        }
    }

    [HarmonyPatch]
    private static class InventorySimulationPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // Cover both UI dispatch overloads and direct synchronous operations
            // (move/swap/split/apply helpers). Never patch open generic methods.
            return new[] { typeof(ItemController), typeof(ItemManipulator) }
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => !m.ContainsGenericParameters
                    && (m.DeclaringType == typeof(ItemManipulator) || m.Name == "ExecutePossibleAction")
                    && m.ReturnType != typeof(void)
                    && !typeof(Task).IsAssignableFrom(m.ReturnType)
                    && !typeof(IEnumerator).IsAssignableFrom(m.ReturnType)
                    && m.GetParameters().Any(p => p.Name == "simulate" && p.ParameterType == typeof(bool)))
                .Cast<MethodBase>();
        }

        [HarmonyPriority(Priority.First)]
#pragma warning disable CA1707 // Identifiers should not contain underscores

        private static void Prefix(bool simulate, out InventorySimulationScope.State __state)
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            __state = InventorySimulationScope.Enter(simulate);
        }

        [HarmonyPriority(Priority.Last)]
#pragma warning disable CA1707 // Identifiers should not contain underscores

        private static void Finalizer(InventorySimulationScope.State __state)
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            // Finalizers run on success and exception; leave the game exception intact.
            InventorySimulationScope.Exit(__state);
        }
    }

    [HarmonyPatch(typeof(ItemFactory), "CreateItem")]
    private static class ItemCreatedPatch
    {
        private static void Postfix(Item __result)
        {
            if (t_pricingDepth == 0 && !InventorySimulationScope.Active && s_captureItems)
                s_pendingBackgrounds.Schedule(__result, true);
        }
    }

    [HarmonyPatch(typeof(Item), "RaiseRefreshEvent")]
    private static class ItemRefreshedPatch
    {
        private static void Prefix(Item __instance) => ItemChanged(__instance);
    }

    [HarmonyPatch(typeof(Item), "set_CurrentAddress")]
    private static class ItemMovedPatch
    {
        private static void Prefix(Item __instance) => ItemChanged(__instance);

        private static void Postfix(Item __instance) => ItemChanged(__instance);
    }

    [HarmonyPatch(typeof(Item), "set_SpawnedInSession")]
    private static class ItemFirPatch
    {
        private static void Postfix(Item __instance) => ItemChanged(__instance);
    }

    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    private static class RaidPreparedPatch
    {
        private static void Postfix()
        {
            s_pendingBackgrounds.ScheduleAll();
            Diagnostics.Report("Price Color Sync: raid item preparation scheduled: " + s_pendingBackgrounds.Count + " known instances.");
        }
    }

    [HarmonyPatch(typeof(ItemView), "UpdateColor")]
    private static class ViewColorPatch
    {
        private static void Prefix(ItemView __instance) => ApplyBackground(__instance);
    }

    [HarmonyPatch(typeof(ItemView), "Kill")]
    private static class ViewKillPatch
    {
        private static void Postfix(ItemView __instance) => UnregisterView(__instance);
    }

    [HarmonyPatch(typeof(SimpleTooltipShowPatch), "ItemMeetsRequirements")]
    private static class CaseTooltipRequirementsPatch
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores

        private static bool Prefix(Item __0, ref bool __result)
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (!IsStorageCase(__0))
                return true;

            // SMTM normally rejects filled containers. We quote only the empty case
            // shell, so keep examination/ownership checks but skip the contents scan.
            try
            {
                if (!TryGetSession(out IEftSession? session) || session!.Profile == null)
                {
                    __result = false;

                    return false;
                }

                IItemOwner? owner = __0.Owner;
                __result = session.Profile.Examined(__0) && owner != null && owner.OwnerType != EOwnerType.Trader;
            }
            catch (Exception exception)
            {
                Warn(exception);
                __result = false;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(SimpleTooltipShowPatch), "ShowTradeAndFleaPriceInformation")]
    private static class TooltipPricePatch
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores

        private static bool Prefix(Item __0, ref StringBuilder __1, ref double? __2)
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            __2 = null;

            // Keep stats in SMTM's replaceable price block so refreshes cannot
            // accumulate lines. A stats failure must not prevent price rendering.
            if (s_showAmmoStats)
            {
                try
                {
                    Ammo? ammo = __0 as Ammo;

                    if (__0 is AmmoBox box)
                        ammo = box.Cartridges.Items.OfType<Ammo>().FirstOrDefault();

                    if (ammo != null)
                        AmmoTooltipStats.Append(__1, EFT.LocalizationExtensions.LocalizedName(__0), ammo.Damage, ammo.PenetrationPower);
                }
                catch (Exception exception)
                {
                    Warn(exception);
                }
            }

            try
            {
                Quote quote = GetQuote(__0, true);
                __2 = quote.Compare;

                if (quote.Price == null || !quote.Compare.HasValue)
                {
                    __1.Append("<br>").Append(quote.UsesFlea ? "Flea estimate unavailable" : "Trader sell price unavailable");
                }
                else
                {
                    // Retain both cached offers. SMTM compares the third argument
                    // to highlight the better offer; null means no competing quote.
                    TradePrice? trader = ComparePrice(quote.Trade!.TraderPrice).HasValue ? quote.Trade.TraderPrice : null;
                    TradePrice? flea = ComparePrice(quote.Trade.FleaPrice).HasValue ? quote.Trade.FleaPrice : null;

                    if (trader != null)
                    {
                        SimpleTooltipShowPatch.ShowPrice(quote.Trade, trader, flea, __1, false);

                        if (!quote.FleaEligible)
                            __1.Append(" (flea banned; trader price)");

                        if (IsStorageCase(__0))
                            __1.Append(" (case only)");
                    }

                    if (flea != null)
                    {
                        SimpleTooltipShowPatch.ShowPrice(quote.Trade, flea, trader, __1, __0 is Weapon);

                        if (!quote.FleaEligible)
                            __1.Append(" (estimate; flea banned)");

                        if (IsStorageCase(__0))
                            __1.Append(" (case only)");
                    }
                }

                RefreshItemViews(__0, quote);
            }
            catch (Exception exception)
            {
                Warn(exception);
                __1.Append("<br>Value unavailable");
            }

            return false;
        }
    }
}
