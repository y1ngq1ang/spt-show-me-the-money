using System.Collections.Generic;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Data;

public class BrokerTraderTrade
{
    public BrokerTraderTrade(string traderId, params TradeItem[] items)
    {
        TraderId = traderId;
        TradeItems.AddRange(items);
    }

    public string TraderId { get; private set; }

    public List<TradeItem> TradeItems { get; private set; } = [];
}
