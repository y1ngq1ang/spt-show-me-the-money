___
> This build targets **SPT 5.0** (compatible with the 5.0.x line). It is not compatible with SPT 4.x.
>
> SPT 5.0 runs the game under **IL2CPP**, so this plugin is a BepInEx 6 plugin built against the
> unhollowed game assemblies. Use the SPT 4.x build for 4.x installs and the SPT 3.11 build for 3.11.
___

___
> **Quick-sell is built in.**  
> Hold a key, click an item, and it is gone — sold to a trader, listed on the flea market, or sent wherever it fetches more. No separate add-on to install any more.
___

## Tabs {.tabset}

### SMTM
#### What does it do?

Imagine examining an item in Tarkov and having the game whisper gently into your ear:  
“Here’s exactly how much money this thing is worth, who will buy it, and whether selling it is a brilliant idea or a catastrophic financial farce.”

That’s what SMTM does. It modifies the in-game tooltip to reveal:

- The trader who will pay you the *most* (because some traders appreciate your junk more than others)  
- The flea market price required to achieve a **100% chance of sale**  
- Both **price per slot** and **total price**, helpfully highlighted so you don’t have to do arithmetic  
- Optional inclusion of flea market taxes, for those who enjoy pain

Version **1.5.0+** adds color-coding based on the World of Warcraft rarity scheme - from *poor* (grey, sad, mildly apologetic) to *legendary* (orange, smug, expensive).

The server-side portion of the mod exposes an endpoint that politely hands the client mod the relevant backend configuration and flea prices.

#### Quick-sell

While the inventory screen is open outside of a raid, hold the quick-sell key and click an item:

- **Q + Left-click** → sell to the trader who pays the most
- **Q + Right-click** → list on the flea market at the price needed for a 100% sale chance
- **Q + Middle-click** → whichever of the two is worth more

The quick-sell add-on used to be a separate download. It is now part of this mod; if you still have `SwiftXP.SPT.ShowMeTheMoney.QuickSell.Client.dll` installed, delete it.

Selecting several items at once works when [UIFixes](https://sp-mod.com/mod/1342/ui-fixes) is installed. Items of the same type are bundled into a single flea offer.

#### Item background colours

Integrated from [Price Color Sync](https://github.com/GPTSol6-1/PriceColorSync). Inventory item backgrounds are tinted using the same valuation the tooltip shows, with Show Me The Money's own tier thresholds and thresholds:

- **Found in raid** items use the higher of the trader and flea offer.
- **Non-FIR** items use the best trader offer by default; the flea estimate can be enabled instead.
- **Flea-banned** items are valued and priced from traders and get a separate red background marker.
- **Filled storage cases** are valued as empty cases, excluding their contents, and the tooltip rows are marked `(case only)`. The live case is never modified.
- **Ammo and filled cartridge packs** can additionally show base damage and penetration.

The background palette, the pricing rules and the per-frame preparation budget all live in the new `8.`–`11.` configuration sections.

Everything is configured through the BepInEx Configurator.  
And yes, the mod is designed to whisper requests to the SPT server, not shout them.

#### Requirements

Practically none. If you can run SPT, you can run this.

#### If you use the FIKA headless client:

Splendid news: **Do not install anything there.** Seriously, don’t. It will only cause complications, headaches, and possibly the rising of ancient eldritch forces. Add this mod to your `Exclusions.json` if you use Corter’s Mod Sync.

#### Configuration

Access the BepInEx Configurator (usually via **F12** or **F1**) and behold:

![BepInEx Plugin Configuration](https://raw.githubusercontent.com/swiftxp-hub/spt-show-me-the-money/refs/heads/main/Assets/plugin-configuration.png)

Here you can also summon fresh flea market prices from the server. This is particularly handy if you’re using **SPT-LiveFleaPrices**, which updates prices regularly but not telepathically.

Just to be clear: Triggering this does **not** cause LiveFleaPrices itself to contact the celestial flea-market-gods. It simply fetches whatever your SPT server already knows.

#### Remarks

- Changes to trader price markups (e.g., via SVM) are automatically accounted for.  
- Multiple languages are supported, but if something looks as if it was spat out by a malfunctioning Babel Fish, leave a comment. Disabling color-coding may help.

#### Known compatibility

- SPT-LiveFleaPrices ≥ v2.0.1 (DrakiaXYZ)  
- UIFixes ≥ v6.0.0 (Tyfon)  
- ODT’s Item Info ≥ v2.0.2 (kobethuy)  
  - Note: ODT’s Item Info has Opinions™ about how color-coding should work. It may overwrite item-name color, but price colors remain intact.  
- All Quests Checkmarks ≥ v1.3.1 (ZGFueDkx)  
- MoreCheckmarks ≥ v2.0.0 (VIPkiller17)  

#### Planned features/changes

- … *(imagine a gentle, wistful sigh here)*

#### Known problems

- A friend of mine occasionally experiences a stuck tooltip when using the flea-tax toggle. I have investigated this thoroughly and discovered that… I still have no idea why it happens.  
- Some weapons feature suffixes like “Carbine.” These sometimes refuse to participate in color-coding. Possibly out of spite.

#### Problems that may occur

- Traders added by other mods may or may not play nicely with the price system.  
- The flea-tax toggle exists to maintain peace between tooltip-altering mods. If something goes wrong, let me know.

### SPT 5.0 Installation  

#### Installation

##### If your SPT installation is normal (i.e. not a baroque cathedral of symbolic links and misplaced folders):

1. Extract the contents of the `.zip` or `.7z` straight into your SPT directory.  
2. When you're done, you should see:
```
- C:\yourSPTfolder\BepInEx\plugins\SwiftXP.SPT.ShowMeTheMoney.Client.dll
- C:\yourSPTfolder\SPT_Runtime\user\mods\swiftxp-showmethemoney\SwiftXP.SPT.ShowMeTheMoney.Server.dll
```

##### If your client and server are separated like two star-crossed lovers:

Extract the **BepInEx** folder into the client.  
Extract the **SPT_Runtime** folder into the server.

Client:
```
- C:\yourSPTclient\BepInEx\plugins\SwiftXP.SPT.ShowMeTheMoney.Client.dll
```

Server:
```
- C:\yourSPTserver\SPT_Runtime\user\mods\swiftxp-showmethemoney\SwiftXP.SPT.ShowMeTheMoney.Server.dll
```

### SPT 3.11.x Installation  

#### Installation

##### If your SPT installation is normal (i.e. not a baroque cathedral of symbolic links and misplaced folders):

1. Extract the contents of the `.zip` or `.7z` straight into your SPT directory.  
2. When you're done, you should see:
```
- C:\yourSPTfolder\BepInEx\plugins\SwiftXP.ShowMeTheMoney.dll  
  
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\package.json
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\src\mod.ts
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\src\models\partialRagfairConfig.ts
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\src\services\fleaPricesService.ts
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\src\services\ragfairConfigService.ts
```

##### If your client and server are separated like two star-crossed lovers:

Extract the **BepInEx** folder into the client.  
Extract the **user** folder into the server.

Client:
```
- C:\yourSPTclient\BepInEx\plugins\SwiftXP.ShowMeTheMoney.dll
```

Server:
```
- C:\yourSPTserver\user\mods\swiftxp-showmethemoney\package.json
- C:\yourSPTserver\user\mods\swiftxp-showmethemoney\src\mod.ts
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\src\models\partialRagfairConfig.ts
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\src\services\fleaPricesService.ts
- C:\yourSPTfolder\user\mods\swiftxp-showmethemoney\src\services\ragfairConfigService.ts
```

### FAQ  
*(Last updated Oct 17, 2026 — SMTM v4.2.0)*

> The armour-plate and key-durability answers below were written against the 4.x line and have not
> been re-verified against SPT 5.0.

- **How do I install SMTM?**  
  - Extract the folders into your SPT directory. If that sounds too simple, consult the installation section(s).

- **How do I configure SMTM?**  
  - Press F12 or F1. A configuration window will appear, as if summoned from a parallel dimension.

- **Is there a quick-sell addon?**  
  - Yes! It has its own tab. It is quite proud of this.

- **No price information appears. What now?**  
  - Check your installation. Reinstall if confused. Also, please don’t use WinRAR to extract `.7z` files. It sometimes mangles them like a clumsy Vogon.

- **No flea market prices?**  
  - You must unlock the flea market unless you enable “Always show flea market price.” If prices are missing entirely, the server component is probably not installed correctly.

- **I get SSL errors. Help?**  
  - Unlikely to be SMTM’s fault. SSL in Tarkov behaves like a paranoid toaster. Seek help on the SPT Discord.

- **What does the `(+)` in the tooltip mean?**  
  - This indicates that the displayed flea market price includes all attachments/mods. If the sign is missing, the price refers only to the base item.

- **Why do flea prices differ from the average?**  
  - Because:
    - SMTM shows the 100% sell-chance price, not the average.  
    - It updates flea data every 5 minutes to avoid server overload.  
    - Live Flea Prices may cause sudden market lurches.  
    - The flea market is fundamentally chaotic, briefly capable of holding an opinion, and possibly drunk.

- **Displayed flea prices seem wildly wrong. Why?**  
  - Check your settings (SPT, SVM, SMTM, etc.). If still wrong, report details: item, expected vs shown price, screenshots, logs, the alignment of Jupiter, etc.

- **Armor plates don’t affect flea price. Why?**  
  - Because SPT 4.0.4 also ignores plate value in its sell-chance logic. SMTM 2.5.0 can display plate value separately. SPT 4.0.5 may fix this.

- **Key remaining-uses don’t affect the price. Why?**  
  - SPT 4.0.4 bug. A 1/40 key and a 40/40 key sell for the same amount. SMTM mimics SPT’s logic.
    **SPT 4.0.5 fixed this bug.** Update for SMTM (2.5.1) is available.

{.endtabset}

---

### Building from source

The projects reference assemblies straight out of an SPT 5.0 install, so point the build at one:

1. Copy `Directory.Build.props.user.example` to `Directory.Build.props.user`.
2. Set `SptRoot` to your SPT 5.0 folder (the one holding `EscapeFromTarkov.exe`). `SPT_ROOT` or
   `/p:SptRoot=...` work too.
3. `dotnet publish -c Release` on each project. Output lands in `Publish/`, laid out the same way as
   the release archive.

The client plugin is `net10.0` and compiles against `BepInEx/core`, `BepInEx/interop` and
`BepInEx/plugins/sptushonka`; the server mod is `net10.0` and compiles against `SPT_Runtime`.

#### Support & Feature Requests

I maintain this in my spare time, between bouts of existential dread and tea.  
Please be patient.

#### Why this mod exists

My friends and I play SPT co-op via FIKA.  
We used another price-display mod that turned our poor little VServer into a molten puddle of lag.  
Requests flooded in like panicked bureaucrats. The server begged for mercy.

So I built a new plugin. This one. Designed to be gentle, efficient, and unlikely to ignite anything.

It worked splendidly, so I shared it.

**Shout-out to the SPT team and all modders.  
You’re brilliant, bewildering, and beloved.**
