using System;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;

public static class TierPolicy
{
    public static bool IsValidPrice(double? value)
    {
        return value.HasValue && !double.IsNaN(value.Value)
            && !double.IsInfinity(value.Value) && value.Value >= 0;
    }

    public static bool PreferTrader(double? flea, double? trader)
    {
        // Preserve the flea source on ties; use an available trader if flea is missing.
        return IsValidPrice(trader) && (!IsValidPrice(flea) || trader!.Value > flea!.Value);
    }

    public static string PriceSource(bool foundInRaid, bool fleaEligible,
        bool fleaForFir = true, bool fleaForNonFir = false, bool traderForBanned = true)
    {
        if (!fleaEligible && traderForBanned)
            return "Trader";

        return (foundInRaid ? fleaForFir : fleaForNonFir) ? "Flea" : "Trader";
    }

    public static int Tier(double value, double[] boundaries)
    {
        if (boundaries == null || boundaries.Length != 5)
            throw new ArgumentException("Five boundaries required");

        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            return -1;

        for (int i = 0; i < boundaries.Length; i++)
        {
            if (value < boundaries[i])
                return i;
        }

        return 5;
    }
}
