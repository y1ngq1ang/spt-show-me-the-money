using BepInEx.Configuration;
using SwiftXP.SPT.Common.ConfigurationManager;
// BepInEx.Configuration.KeyboardShortcut is obsolete under IL2CPP; the supported type lives in the
// IL2CPP assembly and is what the bundled ConfigurationManager knows how to draw.
using KeyboardShortcut = BepInEx.Unity.IL2CPP.Configuration.KeyboardShortcut;
using SwiftXP.SPT.Common.Notifications;
using UnityEngine;
using System;
using SwiftXP.SPT.ShowMeTheMoney.Client.Enums;
using SwiftXP.SPT.ShowMeTheMoney.Client.Services;
using System.Linq;
using System.Collections.Generic;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Configuration;

public class PluginConfiguration
{
    private ConfigEntry<bool> _includeFleaTaxConfigEntry;

    private ConfigEntry<bool> _showFleaTaxConfigEntry;

    public PluginConfiguration(ConfigFile configFile, Action onForceUpdate)
    {
        // --- 1. Main settings
        EnablePlugin = configFile.BindConfiguration("1. 主要设置", "启用插件", true, $"启用或禁用本插件。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 7);
        EnableTraderPrices = configFile.BindConfiguration("1. 主要设置", "显示交易商价格", true, $"在物品提示框中显示交易商收购价格。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 6);
        EnableFleaPrices = configFile.BindConfiguration("1. 主要设置", "显示跳蚤市场价格", true, $"在物品提示框中显示跳蚤市场价格。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 5);
        ShowPricePerSlot = configFile.BindConfiguration("1. 主要设置", "显示每格价格", true, $"在提示框中显示每格价格。即使关闭显示，插件仍会在后台按每格价格进行计算。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 4);
        ShowWeaponModsPrice = configFile.BindConfiguration("1. 主要设置", "显示武器配件总价", true, $"显示武器上所有改装件的价格总和。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 3);
        ShowArmorPlatesPrice = configFile.BindConfiguration("1. 主要设置", "显示防弹插板总价", true, $"显示防弹衣上所有（可拆卸）插板的价格总和。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 2);

        TradersToIgnore = configFile.BindConfiguration("1. 主要设置", "忽略的交易商", "", $"需要忽略的交易商列表，用英文逗号分隔。可以填写交易商 ID 或名称。{Environment.NewLine}{Environment.NewLine}示例：Prapor,Skier,Peacekeeper", 1);

        ToolTipDelay = configFile.BindConfiguration("1. 主要设置", "提示框延迟", 0.0m, $"提示框延迟显示的秒数。{Environment.NewLine}{Environment.NewLine}（插件默认：0，EFT 默认：0.6）", 0);

        // --- 2. Currency conversion
        RoublesOnly = configFile.BindConfiguration("2. 货币换算", "仅显示卢布价格", false, $"只考虑以卢布结算的出售价格。简单来说，不再显示不收购卢布的交易商的报价。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 0);

        // --- 3. Appearance
        BestTradeColor = configFile.BindConfiguration("3. 外观", "最优价格颜色", new Color(1.000f, 1.000f, 1.000f), $"用于高亮显示最优报价（交易商或跳蚤市场）的颜色。{Environment.NewLine}{Environment.NewLine}（默认：R 255, G 255, B 255）", 2);
        FontSize = configFile.BindConfiguration("3. 外观", "字体大小", TooltipFontSize.Smaller, $"调整价格文本的字号。{Environment.NewLine}{Environment.NewLine}（默认：更小）", 1);
        RenderInItalics = configFile.BindConfiguration("3. 外观", "斜体", false, $"以斜体显示价格。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 0);

        // --- 4. Color coding
        EnableColorCoding = configFile.BindConfiguration("4. 颜色编码", "启用颜色编码（基于每格价格）", true, $"用颜色直观地表示物品价值高低。除弹药外，始终基于每格价格；弹药可在下方单独开启相应选项。默认配色取自《魔兽世界》。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 18);
        ColorCodingMode = configFile.BindConfiguration("4. 颜色编码", "颜色编码模式", Enums.ColorCodingMode.Both, $"设置颜色编码的作用对象。{Environment.NewLine}{Environment.NewLine}（默认：两者）", 17);

        PoorValue = configFile.BindConfiguration("4. 颜色编码", "粗糙品质价格上限（低于）", 900m, "（默认：900）", 16);
        CommonValue = configFile.BindConfiguration("4. 颜色编码", "普通品质价格上限（低于）", 12000m, "（默认：12000）", 15);
        UncommonValue = configFile.BindConfiguration("4. 颜色编码", "优秀品质价格上限（低于）", 21000m, "（默认：21000）", 14);
        RareValue = configFile.BindConfiguration("4. 颜色编码", "精良品质价格上限（低于）", 38000m, "（默认：38000）", 13);
        EpicValue = configFile.BindConfiguration("4. 颜色编码", "史诗品质价格上限（低于）——高于此值视为传说品质", 92000m, "（默认：92000）", 12);

        UseCaliberPenetrationPower = configFile.BindConfiguration("4. 颜色编码", "弹药按穿透力而非价格进行颜色编码", true, $"弹药的成色改用穿透力数值判断，而不是价格。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 11);

        PoorPenetrationValue = configFile.BindConfiguration("4. 颜色编码", "粗糙品质穿透力上限（低于）", 15m, "（默认：15）", 10);
        CommonPenetrationValue = configFile.BindConfiguration("4. 颜色编码", "普通品质穿透力上限（低于）", 25m, "（默认：25）", 9);
        UncommonPenetrationValue = configFile.BindConfiguration("4. 颜色编码", "优秀品质穿透力上限（低于）", 34m, "（默认：34）", 8);
        RarePenetrationValue = configFile.BindConfiguration("4. 颜色编码", "精良品质穿透力上限（低于）", 43m, "（默认：43）", 7);
        EpicPenetrationValue = configFile.BindConfiguration("4. 颜色编码", "史诗品质穿透力上限（低于）——高于此值视为传说品质", 55m, "（默认：55）", 6);

        PoorColor = configFile.BindConfiguration("4. 颜色编码", "粗糙品质颜色", new Color(0.62f, 0.62f, 0.62f), "（默认：R 157, G 157, B 157）", 5);
        CommonColor = configFile.BindConfiguration("4. 颜色编码", "普通品质颜色", new Color(1f, 1f, 1f), "（默认：R 255, G 255, B 255）", 4);
        UncommonColor = configFile.BindConfiguration("4. 颜色编码", "优秀品质颜色", new Color(0.12f, 1f, 0f), "（默认：R 30, G 255, B 0）", 3);
        RareColor = configFile.BindConfiguration("4. 颜色编码", "精良品质颜色", new Color(0f, 0.44f, 0.87f), "（默认：R 0, G 112, B 221）", 2);
        EpicColor = configFile.BindConfiguration("4. 颜色编码", "史诗品质颜色", new Color(0.64f, 0.21f, 0.93f), "（默认：R 163, G 53, B 238）", 1);
        LegendaryColor = configFile.BindConfiguration("4. 颜色编码", "传说品质颜色", new Color(1f, 0.5f, 0f), "（默认：R 255, G 128, B 0）", 0);

        // --- 5. Flea market
        AlwaysShowFleaPrice = configFile.BindConfiguration("5. 跳蚤市场", "始终显示跳蚤价格", false, $"即使跳蚤市场尚未解锁，或物品没有「战局中获得」标记且 SPT/SVM 启用了「只能出售带有 'Found in raid' 标记的物品」设置，也始终显示该物品的跳蚤价格。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 8);
        FleaPriceMultiplicand = configFile.BindConfiguration("5. 跳蚤市场", "跳蚤价格系数", 1.0m, $"设置跳蚤市场平均价格在显示前的乘数。计算方式如下：{Environment.NewLine}{Environment.NewLine}物品跳蚤市场平均价格 × 跳蚤价格系数{Environment.NewLine}{Environment.NewLine}（默认：1.0）", 7);
        _includeFleaTaxConfigEntry = configFile.BindConfiguration("5. 跳蚤市场", "价格扣除跳蚤税", false, $"决定跳蚤价格中是否扣除跳蚤市场税费。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 6);
        _showFleaTaxConfigEntry = configFile.BindConfiguration("5. 跳蚤市场", "显示跳蚤税", false, $"在提示框中显示跳蚤市场税费。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 5);
        FleaTaxToggleMode = configFile.BindConfiguration("5. 跳蚤市场", "跳蚤税切换模式", false, $"开启后，只有在按下指定按键或组合键时才显示跳蚤税。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 4);
        FleaTaxToggleKey = configFile.BindConfiguration("5. 跳蚤市场", "切换模式按键", new KeyboardShortcut(KeyCode.LeftAlt), $"设置显示跳蚤税时需要按下的按键或组合键。{Environment.NewLine}{Environment.NewLine}（默认：LeftAlt）", 3);

        UpdateInterval = configFile.BindConfiguration("5. 跳蚤市场", "跳蚤价格更新间隔", 5, $"插件更新跳蚤市场价格的间隔（分钟）。更新越频繁价格越准确，但也会增加服务器负担。{Environment.NewLine}{Environment.NewLine}（默认：5 分钟）", 2);
        UpdateDuringRaid = configFile.BindConfiguration("5. 跳蚤市场", "战局内更新跳蚤价格", false, $"是否在战局进行期间也更新跳蚤市场价格。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 1);

        configFile.CreateButton(
            "5. 跳蚤市场",
            "更新跳蚤价格",
            "立即更新",
            "从你的 SPT 服务器拉取当前跳蚤市场价格（最多可能需要 30 秒）。",
            () =>
            {
                onForceUpdate();
                EftNotificationHelper.SendLongNotice("跳蚤价格将尽快更新……");
            },
            0
        );

        TradersToIgnore.SettingChanged += (_, _) =>
        {
            List<string> tradersToIngore = [.. (TradersToIgnore.GetValue() ?? string.Empty)
                .Trim()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)];

            TraderPriceService.Instance.TradersToIgnore = tradersToIngore;
        };

        FleaTaxToggleMode.SettingChanged += (_, _) =>
        {
            if (FleaTaxToggleMode.IsEnabled())
                Plugin.EnableTooltipUpdatePatch();
            else
                Plugin.DisableTooltipUpdatePatch();
        };

        // --- 6. Quick-sell
        EnableTraderQuickSell = configFile.BindConfiguration("6. 快速出售", "启用快速卖给交易商", true, $"启用快速出售给交易商（鼠标左键）。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 4);
        EnableFleaQuickSell = configFile.BindConfiguration("6. 快速出售", "启用快速上架跳蚤市场", true, $"启用快速上架到跳蚤市场（鼠标右键）。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 3);
        EnableBestTradeQuickSell = configFile.BindConfiguration("6. 快速出售", "启用快速按最优价格出售", true, $"启用按两者中较高的价格快速出售（鼠标中键）。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 2);
        QuickSellKey = configFile.BindConfiguration("6. 快速出售", "快速出售按键", new KeyboardShortcut(KeyCode.Q), $"设置快速出售物品时，需要与鼠标左键、中键或右键组合按下的按键或组合键。{Environment.NewLine}{Environment.NewLine}（默认：Q）", 1);

        // --- 7. Quick-sell: flea market limits
        SellToTraderIfFleaSlotsFull = configFile.BindConfiguration("7. 快速出售：跳蚤市场", "跳蚤槽位用尽时改卖交易商", false, $"当跳蚤市场没有可用槽位时，改为把物品卖给交易商。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 4);
        AllowAnyNumberOfFleaOffers = configFile.BindConfiguration("7. 快速出售：跳蚤市场", "允许任意数量的跳蚤挂单", false, $"允许在跳蚤市场挂出任意数量的物品，忽略塔科夫原本的限制。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 3);
        DoNotSellFoundInRaidItems = configFile.BindConfiguration("7. 快速出售：跳蚤市场", "不出售战局中获得的物品（FIR）", false, $"禁止出售在战局中获得的物品。仍可通过下方设置的按键强制出售这类物品。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 2);
        ForceSellFoundInRaidItemsKey = configFile.BindConfiguration("7. 快速出售：跳蚤市场", "强制出售 FIR 物品按键", new KeyboardShortcut(KeyCode.Q, KeyCode.LeftAlt), $"设置强制出售「战局中获得」物品时，需要与鼠标左键、中键或右键组合按下的按键或组合键，忽略其 FIR 状态。仅在启用「不出售战局中获得的物品（FIR）」时有效。{Environment.NewLine}{Environment.NewLine}（默认：Q + LeftAlt）", 1);

        // --- 8. Price Color Sync
        PriceColorSyncEnabled = configFile.BindConfiguration("8. 价格颜色同步", "启用价格颜色同步", true, $"让背包物品背景色与提示框价格使用同一套估值，并接管提示框中的价格比较显示。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 2);
        PriceColorSyncShowAmmoStats = configFile.BindConfiguration("8. 价格颜色同步", "显示弹药伤害与穿透", true, $"在弹药和装有弹药的弹匣的提示框价格上方显示基础伤害与穿透力。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 1);

        // --- 9. Price Color Sync: pricing rules
        PriceColorSyncFleaForFir = configFile.BindConfiguration("9. 价格颜色同步：定价规则", "FIR 物品使用跳蚤估价", true, $"对「战局中获得」的物品比较跳蚤估价与最高交易商收购价，取其中较高的值。关闭后仅使用交易商价格。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 2);
        PriceColorSyncFleaForNonFir = configFile.BindConfiguration("9. 价格颜色同步：定价规则", "非 FIR 物品使用跳蚤估价", false, $"对非 FIR 物品也使用跳蚤估价。关闭时使用最高交易商收购价。{Environment.NewLine}{Environment.NewLine}（默认：禁用）", 1);
        PriceColorSyncTraderForBanned = configFile.BindConfiguration("9. 价格颜色同步：定价规则", "跳蚤禁售物品使用交易商价格", true, $"对无法上架跳蚤市场的物品覆盖上面两条规则，改用交易商价格。关闭后允许使用缓存的跳蚤估价。{Environment.NewLine}{Environment.NewLine}（默认：启用）", 0);

        // --- 10. Price Color Sync: background palette
        PriceColorSyncTier0 = configFile.BindConfiguration("10. 价格颜色同步：物品背景色", "档位 0（最差）", "#404040", $"物品背景色（RGB）。档位从粗糙到传说依次递增。{Environment.NewLine}{Environment.NewLine}（默认：#404040）", 7);
        PriceColorSyncTier1 = configFile.BindConfiguration("10. 价格颜色同步：物品背景色", "档位 1", "#a3a3a3", "物品背景色（RGB）。（默认：#a3a3a3）", 6);
        PriceColorSyncTier2 = configFile.BindConfiguration("10. 价格颜色同步：物品背景色", "档位 2", "#0c3b08", "物品背景色（RGB）。（默认：#0c3b08）", 5);
        PriceColorSyncTier3 = configFile.BindConfiguration("10. 价格颜色同步：物品背景色", "档位 3", "#08083b", "物品背景色（RGB）。（默认：#08083b）", 4);
        PriceColorSyncTier4 = configFile.BindConfiguration("10. 价格颜色同步：物品背景色", "档位 4", "#590b5e", "物品背景色（RGB）。（默认：#590b5e）", 3);
        PriceColorSyncTier5 = configFile.BindConfiguration("10. 价格颜色同步：物品背景色", "档位 5（最好）", "#5e470b", "物品背景色（RGB）。（默认：#5e470b）", 2);
        PriceColorSyncFleaBannedBackground = configFile.BindConfiguration("10. 价格颜色同步：物品背景色", "跳蚤禁售背景色", "#660415", $"跳蚤市场禁售物品的背景标记色（RGB），与价格档位无关。弹药不适用。{Environment.NewLine}{Environment.NewLine}（默认：#660415）", 1);

        // --- 11. Price Color Sync: performance
        PriceColorSyncMaxItemsPerFrame = configFile.Bind("11. 价格颜色同步：性能", "每帧最多处理物品数", 12,
            new ConfigDescription("每帧提前准备估价的物品数量上限，范围 1–64。首次显示时的冷估价不受此限制。",
                new AcceptableValueRange<int>(1, 64),
                new ConfigurationManagerAttributes { Order = 1 }));
        PriceColorSyncBudgetMilliseconds = configFile.Bind("11. 价格颜色同步：性能", "后台处理时间预算（毫秒）", 2f,
            new ConfigDescription("每帧用于提前准备和重新着色的时间预算，范围 0.25–10 毫秒。单个物品可能超出该预算。",
                new AcceptableValueRange<float>(0.25f, 10f),
                new ConfigurationManagerAttributes { Order = 0 }));

        configFile.SaveOnConfigSet = true;
    }

    #region Main settings 
    public ConfigEntry<bool> EnablePlugin { get; set; }

    public ConfigEntry<bool> EnableTraderPrices { get; set; }

    public ConfigEntry<bool> EnableFleaPrices { get; set; }

    public ConfigEntry<bool> ShowPricePerSlot { get; set; }

    public ConfigEntry<bool> ShowWeaponModsPrice { get; set; }

    public ConfigEntry<bool> ShowArmorPlatesPrice { get; set; }

    public ConfigEntry<string> TradersToIgnore { get; set; }

    public ConfigEntry<decimal> ToolTipDelay { get; set; }
    #endregion

    #region Currency conversion
    public ConfigEntry<bool> RoublesOnly { get; set; }
    #endregion

    #region Appearance
    public ConfigEntry<Color> BestTradeColor { get; set; }

    public ConfigEntry<TooltipFontSize> FontSize { get; set; }

    public ConfigEntry<bool> RenderInItalics { get; set; }
    #endregion

    #region Color coding
    public ConfigEntry<bool> EnableColorCoding { get; set; }

    public ConfigEntry<ColorCodingMode> ColorCodingMode { get; set; }

    public ConfigEntry<decimal> PoorValue { get; set; }

    public ConfigEntry<decimal> CommonValue { get; set; }

    public ConfigEntry<decimal> UncommonValue { get; set; }

    public ConfigEntry<decimal> RareValue { get; set; }

    public ConfigEntry<decimal> EpicValue { get; set; }

    public ConfigEntry<bool> UseCaliberPenetrationPower { get; set; }

    public ConfigEntry<decimal> PoorPenetrationValue { get; set; }

    public ConfigEntry<decimal> CommonPenetrationValue { get; set; }

    public ConfigEntry<decimal> UncommonPenetrationValue { get; set; }

    public ConfigEntry<decimal> RarePenetrationValue { get; set; }

    public ConfigEntry<decimal> EpicPenetrationValue { get; set; }

    public ConfigEntry<Color> PoorColor { get; set; }

    public ConfigEntry<Color> CommonColor { get; set; }

    public ConfigEntry<Color> UncommonColor { get; set; }

    public ConfigEntry<Color> RareColor { get; set; }

    public ConfigEntry<Color> EpicColor { get; set; }

    public ConfigEntry<Color> LegendaryColor { get; set; }
    #endregion

    #region Flea market
    public ConfigEntry<bool> AlwaysShowFleaPrice { get; set; }

    public ConfigEntry<decimal> FleaPriceMultiplicand { get; set; }

    public bool IncludeFleaTax
    {
        get
        {
            if (FleaTaxToggleMode.IsEnabled())
            {
                return _includeFleaTaxConfigEntry.IsEnabled()
                    && FleaTaxToggleKey.GetValue().IsPressed();
            }

            return _includeFleaTaxConfigEntry.IsEnabled();
        }
        set
        {
            _includeFleaTaxConfigEntry.Value = value;
        }
    }

    public bool ShowFleaTax
    {
        get
        {
            if (FleaTaxToggleMode.IsEnabled())
            {
                return _showFleaTaxConfigEntry.IsEnabled()
                    && FleaTaxToggleKey.GetValue().IsPressed();
            }

            return _showFleaTaxConfigEntry.IsEnabled();
        }
        set
        {
            _showFleaTaxConfigEntry.Value = value;
        }
    }

    public ConfigEntry<bool> FleaTaxToggleMode { get; set; }

    public ConfigEntry<KeyboardShortcut> FleaTaxToggleKey { get; set; }

    public ConfigEntry<int> UpdateInterval { get; set; }

    public ConfigEntry<bool> UpdateDuringRaid { get; set; }
    #endregion

    #region Quick-sell
    public ConfigEntry<bool> EnableTraderQuickSell { get; set; }

    public ConfigEntry<bool> EnableFleaQuickSell { get; set; }

    public ConfigEntry<bool> EnableBestTradeQuickSell { get; set; }

    public ConfigEntry<KeyboardShortcut> QuickSellKey { get; set; }

    public ConfigEntry<bool> SellToTraderIfFleaSlotsFull { get; set; }

    public ConfigEntry<bool> AllowAnyNumberOfFleaOffers { get; set; }

    public ConfigEntry<bool> DoNotSellFoundInRaidItems { get; set; }

    public ConfigEntry<KeyboardShortcut> ForceSellFoundInRaidItemsKey { get; set; }
    #endregion

    #region Price Color Sync
    public ConfigEntry<bool> PriceColorSyncEnabled { get; set; }

    public ConfigEntry<bool> PriceColorSyncShowAmmoStats { get; set; }

    public ConfigEntry<bool> PriceColorSyncFleaForFir { get; set; }

    public ConfigEntry<bool> PriceColorSyncFleaForNonFir { get; set; }

    public ConfigEntry<bool> PriceColorSyncTraderForBanned { get; set; }

    public ConfigEntry<string> PriceColorSyncTier0 { get; set; }

    public ConfigEntry<string> PriceColorSyncTier1 { get; set; }

    public ConfigEntry<string> PriceColorSyncTier2 { get; set; }

    public ConfigEntry<string> PriceColorSyncTier3 { get; set; }

    public ConfigEntry<string> PriceColorSyncTier4 { get; set; }

    public ConfigEntry<string> PriceColorSyncTier5 { get; set; }

    public ConfigEntry<string> PriceColorSyncFleaBannedBackground { get; set; }

    public ConfigEntry<int> PriceColorSyncMaxItemsPerFrame { get; set; }

    public ConfigEntry<float> PriceColorSyncBudgetMilliseconds { get; set; }
    #endregion
}