using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using EFT.Trading;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.Common.Constants;
using SwiftXP.SPT.Common.Sessions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data;
using SwiftXP.SPT.ShowMeTheMoney.Client.Extensions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Utilities;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Services;

public class TraderPriceService
{
    private static readonly Lazy<TraderPriceService> s_instance = new(() => new TraderPriceService());

    private TraderPriceService() { }

    /// <summary>
    /// Warms every trader's supply data. Scheduled by <see cref="Patches.MenuReadyPatch" /> once the
    /// main menu is up; doing it any earlier stalls the menu.
    /// </summary>
    public static void PrefetchSupplyData()
    {
        try
        {
            foreach (Trader trader in SptSession.Session.Traders)
                trader.UpdateSupplyData();
        }
        catch (Exception exception)
        {
            PluginContextHolder.Current.SptLogger?
                .LogException(exception);
        }
    }

    public bool GetBestTraderPrice(TradeItem tradeItem)
    {
        TradePrice? highestTraderPrice = null;
        foreach (Trader trader in SptSession.Session.Traders)
        {
            if (IsTraderAvailable(trader))
            {
                // Self-healing path in case the menu hook did not run. GetUserItemPrice needs the
                // supply data, so there is nothing to compute until it arrives.
                if (trader._supplyData is null)
                {
                    trader.UpdateSupplyData();
                    continue;
                }

                bool hasPrice = TryGetTraderUserItemPrice(trader, tradeItem, out Trader.ItemPrice? singleObjectPrice, out Trader.ItemPrice? totalPrice);
                if (hasPrice && (!PluginContextHolder.Current!.Configuration!.RoublesOnly.IsEnabled() || PaysInRoubles(singleObjectPrice!)))
                {
                    MongoID? currencyId = GetCurrencyId(singleObjectPrice!);
                    double? currencyCourse = GetCurrencyCourse(trader, currencyId);

                    int? totalItemPrice = totalPrice?.Amount;

                    TradePrice traderPrice = new(
                        tradeItem,
                        trader.Id,
                        trader.LocalizedName,
                        singleObjectPrice!.Amount,
                        totalItemPrice,
                        currencyCourse,
                        currencyId
                    );

                    if (highestTraderPrice == null || traderPrice.GetComparePriceInRouble() > highestTraderPrice.GetComparePriceInRouble())
                        highestTraderPrice = traderPrice;
                }
            }
        }

        tradeItem.TraderPrice = highestTraderPrice;

        return tradeItem.TraderPrice is not null;
    }

    private bool IsTraderAvailable(Trader trader)
    {
        bool isAvailable = trader.Info.Available && !trader.Info.Disabled && trader.Info.Unlocked;
        bool isIgnored = TradersToIgnore.Any(
            x => x.Equals(trader.Id, StringComparison.OrdinalIgnoreCase)
            || x.Equals(trader.LocalizedName, StringComparison.OrdinalIgnoreCase));

        return isAvailable && !isIgnored;
    }

    /// <remarks>
    /// <see cref="Trader.GetUserItemPrice(Item)" /> hands back an <c>Il2CppSystem.Nullable&lt;ItemPrice&gt;</c>
    /// rather than a real <c>ItemPrice?</c>, so the two are unwrapped here and the rest of the service can
    /// keep treating "no price" as <see langword="null" />.
    /// </remarks>
    private static bool TryGetTraderUserItemPrice(Trader trader, TradeItem tradeItem,
        out Trader.ItemPrice? singleObjectPrice, out Trader.ItemPrice? totalPrice)
    {
        singleObjectPrice = null;
        totalPrice = null;

        try
        {
            Item singleItem = tradeItem.Item.CloneForPricing();
            singleItem.StackObjectsCount = 1;

            Il2CppSystem.Nullable<Trader.ItemPrice> singleResult = trader.GetUserItemPrice(singleItem);
            if (singleResult.HasValue)
                singleObjectPrice = singleResult.Value;

            Il2CppSystem.Nullable<Trader.ItemPrice> totalResult = trader.GetUserItemPrice(tradeItem.Item);
            if (totalResult.HasValue)
                totalPrice = totalResult.Value;
        }
        catch (Exception)
        {
            PluginContextHolder.Current.SptLogger?
                .LogDebug($"Could not get price from trader \"{trader.LocalizedName}\". Skipping.");
        }

        return singleObjectPrice is not null;
    }

    private static bool PaysInRoubles(Trader.ItemPrice price)
    {
        return price.CurrencyId.HasValue
            && price.CurrencyId.Value.ToString() == SptConstants.CurrencyIds.Roubles;
    }

    private static MongoID? GetCurrencyId(Trader.ItemPrice price)
    {
        return price.CurrencyId.HasValue ? price.CurrencyId.Value : null;
    }

    private static double? GetCurrencyCourse(Trader trader, MongoID? currencyId)
    {
        if (currencyId is null)
            return null;

        if (trader.CurrencyCourses != null && trader.CurrencyCourses.TryGetValue(currencyId.ToString(), out double course))
            return course;

        double? result = trader.GetSupplyData()?.CurrencyCourses[currencyId.ToString()];

        return result ?? 1;
    }

    public static TraderPriceService Instance => s_instance.Value;

    public List<string> TradersToIgnore { get; set; } = [];
}
