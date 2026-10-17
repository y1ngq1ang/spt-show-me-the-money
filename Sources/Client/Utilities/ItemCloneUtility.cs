using EFT.InventoryLogic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Utilities;

public static class ItemCloneUtility
{
    /// <summary>
    /// Clones an item so it can be mutated for pricing without touching the real instance.
    /// </summary>
    /// <remarks>
    /// The old hand-rolled <c>TemporaryIdGenerator</c> is gone: <see cref="ItemExtensions.CloneItem{T}(T, IDatabaseIdGenerator)" />
    /// takes an optional id generator, and passing none is the "hand every node a fresh id" path —
    /// <c>CloneItemWithSameId</c> exists precisely to opt out of it. Implementing
    /// <c>IDatabaseIdGenerator</c> ourselves is no longer possible anyway, since under IL2CPP it is
    /// an injected interface that only real game or class-injected types can satisfy.
    /// </remarks>
    public static T CloneForPricing<T>(this T item, bool stripChildSlots = false) where T : Item
    {
        T clone = item.CloneItem();
        if (stripChildSlots && clone is CompoundItem compoundItem)
            compoundItem.Slots = new Il2CppReferenceArray<Slot>(0);

        return clone;
    }
}
