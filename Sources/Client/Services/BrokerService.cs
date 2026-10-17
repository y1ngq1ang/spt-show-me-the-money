using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Trading;
using EFT.UI;
using EFT.UI.Ragfair;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.Common.Constants;
using SwiftXP.SPT.Common.Notifications;
using SwiftXP.SPT.Common.Sessions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data;
using SwiftXP.SPT.ShowMeTheMoney.Client.Enums;
using SwiftXP.SPT.ShowMeTheMoney.Client.Extensions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Utilities;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Services;

public static class BrokerService
{
    public static void Trade(BrokerTradeType brokerTradeType, params Item[] items)
    {
        bool soldAnyThing = false;

        try
        {
            if (!SptSession.Session.RagFair.Available && (brokerTradeType == BrokerTradeType.Flea || brokerTradeType == BrokerTradeType.Best))
                EftNotificationHelper.SendLongAlert("ragfair/Unlocked at character LVL {0}".Localized(null).Replace("{0}",
                    RagFair.Settings.minUserLevel.ToString(CultureInfo.InvariantCulture)));

            List<TradeItem> tradeItems = GetTradeItems(brokerTradeType, items);

            List<BrokerFleaTrade> brokerFleaTrades = GetBrokerFleaTrades(tradeItems);
            List<BrokerTraderTrade> brokerTraderTrades = GetBrokerTraderTrades(tradeItems);

            if (tradeItems.Count != 0)
            {
                switch (brokerTradeType)
                {
                    case BrokerTradeType.Trader:
                        EftNotificationHelper.SendLongAlert("Trader can't buy this item".Localized(null));
                        break;

                    case BrokerTradeType.Flea:
                        EftNotificationHelper.SendLongAlert("ragfair/This item cannot be placed at ragfair".Localized(null));
                        break;

                    case BrokerTradeType.Best:
                        EftNotificationHelper.SendLongAlert("Not all items could be sold.");
                        break;
                }
            }

            foreach (BrokerFleaTrade brokerFleaTrade in brokerFleaTrades)
            {
                SellItemsOnFlea(brokerFleaTrade.Price, [.. brokerFleaTrade.TradeItems]);
            }

            foreach (BrokerTraderTrade brokerTraderTrade in brokerTraderTrades)
            {
                SellItemsToTrader(brokerTraderTrade.TraderId, [.. brokerTraderTrade.TradeItems]);
            }

            soldAnyThing = brokerFleaTrades.Count != 0 || brokerTraderTrades.Count != 0;
        }
        finally
        {
            if (soldAnyThing)
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.TradeOperationComplete);
            }
        }
    }

    private static List<TradeItem> GetTradeItems(BrokerTradeType brokerTradeType, Item[] items)
    {
        List<TradeItem> tradeItems = [];

        foreach (Item item in items)
        {
            TradeItem tradeItem = new(item);

            if (brokerTradeType == BrokerTradeType.Trader || brokerTradeType == BrokerTradeType.Best)
                TraderPriceService.Instance.GetBestTraderPrice(tradeItem);

            if (SptSession.Session.RagFair.Available && (brokerTradeType == BrokerTradeType.Flea || brokerTradeType == BrokerTradeType.Best))
                FleaPriceUtility.GetFleaPrice(tradeItem, true);

            tradeItems.Add(tradeItem);
        }

        return tradeItems;
    }

    private static List<BrokerFleaTrade> GetBrokerFleaTrades(List<TradeItem> tradeItems)
    {
        List<BrokerFleaTrade> result = [];

        GetFleaSlotsForUser(out int currentOffersCount, out int maxOffersCount);

        foreach (TradeItem tradeItem in tradeItems.OrderByDescending(x => x.FleaPrice?.SingleObjectPrice ?? int.MinValue))
        {
            if (tradeItem.Item.CanSellOnRagfair && tradeItem.FleaPrice != null
                && (!RagFair.Settings.isOnlyFoundInRaidAllowed || (RagFair.Settings.isOnlyFoundInRaidAllowed && tradeItem.Item.MarkedAsSpawnedInSession))
                && (tradeItem.TraderPrice is null || tradeItem.FleaPrice.GetComparePriceInRouble() > tradeItem.TraderPrice.GetComparePriceInRouble()))
            {
                if (result.Any(x => x.ItemTemplateId == tradeItem.Item.TemplateId))
                {
                    result.First(x => x.ItemTemplateId == tradeItem.Item.TemplateId).TradeItems.Add(tradeItem);
                }
                else if (PluginContextHolder.Current.Configuration!.AllowAnyNumberOfFleaOffers.IsEnabled() || currentOffersCount < maxOffersCount)
                {
                    result.Add(new(tradeItem.Item.TemplateId, tradeItem.FleaPrice!.SingleObjectPrice, tradeItem));
                    ++currentOffersCount;
                }
            }
        }

        foreach (TradeItem tradeItem in result.SelectMany(x => x.TradeItems))
        {
            tradeItems.Remove(tradeItem);
        }

        return result;
    }

    private static List<BrokerTraderTrade> GetBrokerTraderTrades(List<TradeItem> tradeItems)
    {
        List<BrokerTraderTrade> result = [];

        foreach (TradeItem tradeItem in tradeItems.OrderByDescending(x => x.FleaPrice?.SingleObjectPrice ?? int.MinValue))
        {
            if (tradeItem.TraderPrice != null
                && (tradeItem.FleaPrice is null
                    || tradeItem.TraderPrice.GetComparePriceInRouble() > tradeItem.FleaPrice.GetComparePriceInRouble()
                    || (RagFair.Settings.isOnlyFoundInRaidAllowed && !tradeItem.Item.MarkedAsSpawnedInSession)
                    || PluginContextHolder.Current.Configuration!.SellToTraderIfFleaSlotsFull.IsEnabled()))
            {
                if (result.Any(x => x.TraderId == tradeItem.TraderPrice.TraderId))
                {
                    result.First(x => x.TraderId == tradeItem.TraderPrice.TraderId).TradeItems.Add(tradeItem);
                }
                else
                {
                    result.Add(new(tradeItem.TraderPrice!.TraderId!, tradeItem));
                }
            }
        }

        foreach (TradeItem tradeItem in result.SelectMany(x => x.TradeItems))
        {
            tradeItems.Remove(tradeItem);
        }

        return result;
    }

    private static void SellItemsToTrader(string traderId, TradeItem[] tradeItems)
    {
        Trader trader = ((ITradingSession)SptSession.Session).GetTrader(traderId);

        int totalPrice = tradeItems.Sum(x => x.TraderPrice!.TotalPrice ?? GetTraderItemPrice(trader, x));

        Il2CppReferenceArray<TradingItemReference> references = new(
            [.. tradeItems.Select(x => new TradingItemReference { Item = x.Item, Count = x.Item.StackObjectsCount })]);

        // Il2CppInterop turns delegates into IL2CPP objects; the implicit conversion from a managed
        // delegate is the supported way to hand one to the game.
        Callback callback = new System.Action<IResult>(_ => { });

        ((ITradingSession)SptSession.Session).ConfirmSell(
            traderId,
            references,
            totalPrice,
            callback
        );
    }

    /// <remarks>
    /// GetUserItemPrice returns the IL2CPP proxy of <c>Nullable&lt;ItemPrice&gt;</c>, not a real
    /// nullable value type, so it has to be unwrapped rather than null-forgiven.
    /// </remarks>
    private static int GetTraderItemPrice(Trader trader, TradeItem tradeItem)
    {
        Il2CppSystem.Nullable<Trader.ItemPrice> price = trader.GetUserItemPrice(tradeItem.Item);

        return price.HasValue ? price.Value.Amount : 0;
    }

    private static void SellItemsOnFlea(int price, TradeItem[] tradeItems)
    {
        Il2CppStringArray itemIds = new([.. tradeItems.Select(x => x.Item.Id.ToString())]);

        Il2CppReferenceArray<BarterTemplate> requirements = new(
        [
            new BarterTemplate
            {
                count = price,
                _tpl = SptConstants.CurrencyIds.Roubles
            }
        ]);

        Il2CppSystem.Action callback = new System.Action(() => { });

        SptSession.Session.RagFair.AddOffer(
            false,
            itemIds,
            requirements,
            callback
        );
    }

    private static void GetFleaSlotsForUser(out int currentOffersCount, out int maxOffersCount)
    {
        currentOffersCount = SptSession.Session.RagFair.MyOffersCount;
        maxOffersCount = SptSession.Session.RagFair.GetMaxOffersCount(SptSession.Session.RagFair.MyRating);
    }
}
