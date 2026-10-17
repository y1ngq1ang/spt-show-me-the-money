using System.Collections.Generic;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Data;

public record PartialRagfairConfig
{
    public int Base { get; set; }

    /// <summary>
    /// Item template id to price multiplier.
    /// </summary>
    /// <remarks>
    /// Keyed by string rather than <c>MongoID</c> because that is what the server puts on the wire
    /// (<c>ragfairConfig.Dynamic.ItemPriceMultiplier.ToDictionary(x =&gt; x.Key.ToString(), ...)</c>),
    /// and because <c>System.Text.Json</c> would otherwise need a custom dictionary key converter for
    /// the interop <c>MongoID</c> proxy type. Callers pass a <c>MongoID</c> template id, which converts
    /// implicitly.
    /// </remarks>
    public Dictionary<string, double>? ItemPriceMultiplier { get; set; }

    public int MaxSellChancePercent { get; set; }

    public double SellMultiplier { get; set; }
}
