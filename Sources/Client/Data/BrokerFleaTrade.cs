using System.Collections.Generic;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Data;

public class BrokerFleaTrade
{
    public BrokerFleaTrade(string itemTemplateId, int price, params TradeItem[] items)
    {
        ItemTemplateId = itemTemplateId;
        Price = price;
        TradeItems.AddRange(items);
    }

    public string ItemTemplateId { get; private set; }

    public int Price { get; private set; }

    public List<TradeItem> TradeItems { get; private set; } = [];
}
