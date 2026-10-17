using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.Common.EFT;
using SwiftXP.SPT.Common.Sessions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Enums;
using SwiftXP.SPT.ShowMeTheMoney.Client.Services;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Patches;

public class GridItemViewOnClickPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(typeof(GridItemView), x => x.Name == nameof(GridItemView.OnClick));

    [PatchPrefix]
#pragma warning disable CA1707 // Identifiers should not contain underscores

    public static bool Prefix(GridItemView __instance, PointerEventData.InputButton button, Vector2 position, bool doubleClick)
#pragma warning restore CA1707 // Identifiers should not contain underscores

    {
        bool result = true;

        try
        {
            if (!PluginContextHolder.Current.IsInInventoryScreen
                || __instance == null
                || __instance.Item == null
                || !PluginContextHolder.Current.Configuration!.EnablePlugin.IsEnabled()
                || !IsQuickSellKeyPressed()
                || EFTHelper.IsInRaid)
            {
                return true;
            }

            List<Item> itemsToSell = GetItemsToSell(__instance);

            if (itemsToSell.Count != 0)
            {
                switch (button)
                {
                    case PointerEventData.InputButton.Left:
                        if (PluginContextHolder.Current.Configuration!.EnableTraderQuickSell.IsEnabled())
                        {
                            BrokerService.Trade(BrokerTradeType.Trader, [.. itemsToSell]);
                            result = false;
                        }

                        break;

                    case PointerEventData.InputButton.Right:
                        if (PluginContextHolder.Current.Configuration!.EnableFleaQuickSell.IsEnabled())
                        {
                            BrokerService.Trade(BrokerTradeType.Flea, [.. itemsToSell]);
                            result = false;
                        }

                        break;

                    case PointerEventData.InputButton.Middle:
                        if (PluginContextHolder.Current.Configuration!.EnableBestTradeQuickSell.IsEnabled())
                        {
                            BrokerService.Trade(BrokerTradeType.Best, [.. itemsToSell]);
                            result = false;
                        }

                        break;

                    default:
                        break;
                }
            }

            if (!result)
            {
                PluginContextHolder.SetHoveredItem(null);
                SimpleTooltipShowPatch.Instance?.Close();
            }
        }
        catch (Exception exception)
        {
            PluginContextHolder.Current.SptLogger?
                .LogException(exception);
        }

        return result;
    }

    private static List<Item> GetItemsToSell(GridItemView __instance)
    {
        List<Item> itemsToSell = [];

        if (UIFixesInterop.MultiSelect.Count > 0)
            itemsToSell.AddRange([.. UIFixesInterop.MultiSelect.Items]);

        else if (__instance?.Item is not null)
            itemsToSell.Add(__instance.Item);

        List<Item> sellableItems = [];

        foreach (Item itemToSell in itemsToSell)
        {
            if (ItemMeetsRequirements(itemToSell))
            {
                sellableItems.Add(itemToSell);
            }
        }

        return sellableItems;
    }

    private static bool ItemMeetsRequirements(Item item)
    {
        return SptSession.Session.Profile.Examined(item)
            && (!item.IsContainer || (item.IsContainer && item.IsEmpty()))
            && item.PinLockState != EItemPinLockState.Locked
            && ItemIsFirAndAllowedToBeSold(item)
            && item.Owner.OwnerType != EOwnerType.Trader;
    }

    private static bool ItemIsFirAndAllowedToBeSold(Item item)
    {
        if (PluginContextHolder.Current.Configuration!.DoNotSellFoundInRaidItems.IsEnabled()
            && item.MarkedAsSpawnedInSession
            && !PluginContextHolder.Current.Configuration!.ForceSellFoundInRaidItemsKey.Value.IsPressed())
        {
            return false;
        }

        return true;
    }

    private static bool IsQuickSellKeyPressed()
    {
        if (PluginContextHolder.Current.Configuration!.QuickSellKey.Value.IsPressed())
            return true;

        if (PluginContextHolder.Current.Configuration!.DoNotSellFoundInRaidItems.IsEnabled()
            && PluginContextHolder.Current.Configuration!.ForceSellFoundInRaidItemsKey.Value.IsPressed())
            return true;

        return false;
    }
}
