namespace SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;

// Inventory previews mutate live addresses and then roll back synchronously.
// A real inner Add/Remove must inherit the outer simulation's suppression.
internal static class InventorySimulationScope
{
    [System.ThreadStatic]
    private static int s_depth;

    internal struct State
    {
        internal bool Entered;
        internal int PreviousDepth;
    }

    internal static bool Active => s_depth != 0;

    internal static State Enter(bool simulate)
    {
        if (!simulate)
            return default;

        State state = new() { Entered = true, PreviousDepth = s_depth };
        s_depth++;
        return state;
    }

    internal static void Exit(State state)
    {
        // Default state is possible if an earlier Harmony prefix throws.
        if (state.Entered)
            s_depth = state.PreviousDepth;
    }
}
