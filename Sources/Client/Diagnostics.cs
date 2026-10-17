using System.Collections.Generic;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client;

/// <summary>
/// TEMPORARY: one-shot trace logging used to pin down a main-menu loading stall under SPT 5.0.
/// Remove this file and its call sites once the cause is identified.
/// </summary>
internal static class Diagnostics
{
    private static readonly HashSet<string> s_reported = [];

    /// <summary>
    /// Logs <paramref name="message" /> the first time <paramref name="key" /> is seen.
    /// </summary>
    public static void ReportOnce(string key, string message)
    {
        if (s_reported.Add(key))
            PluginContextHolder.Current?.SptLogger?.LogInfo($"{Constants.LoggerPrefix}{message}");
    }

    public static void Report(string message)
    {
        PluginContextHolder.Current?.SptLogger?.LogInfo($"{Constants.LoggerPrefix}{message}");
    }
}
