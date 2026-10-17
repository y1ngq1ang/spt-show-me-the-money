using BepInEx.Unity.IL2CPP;
using Comfort.Common;
using EFT;
using SPTushonka.Reflection.Utils;

namespace SwiftXP.SPT.Common.EFT;

public static class EFTGameExtensions
{
    public static bool IsFikaHeadlessInstalled()
    {
        // BepInEx 6 has no static Chainloader.PluginInfos; the IL2CPP chainloader is a singleton.
        return IL2CPPChainloader.Instance.Plugins.ContainsKey("com.fika.headless");
    }

    public static bool IsInRaid
    {
        get
        {
            bool? inRaid = Singleton<AbstractGame>.Instance?.InRaid;

            return inRaid.HasValue && inRaid.Value;
        }
    }

    // SPT 5.0 dropped the EftClientBackendSession class; the session is only reachable through
    // the IEftSession interface now.
    public static IEftSession Session => ClientAppUtils.GetMainApp().GetClientBackEndSession();
}

public static class EFTHelper
{
    public static bool IsInRaid => EFTGameExtensions.IsInRaid;
}
