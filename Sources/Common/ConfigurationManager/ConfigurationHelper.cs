using System;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace SwiftXP.SPT.Common.ConfigurationManager;

public static class ConfigurationHelper
{
    static ConfigurationHelper()
    {
        RegisterColorConverter();
    }

    /// <summary>
    /// Teaches BepInEx 6's config system about <see cref="Color" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ConfigFile.Bind{T}" /> rejects any type whose converter is not registered, and
    /// <c>UnityEngine.Color</c> is not a BepInEx built-in. The 4.x builds got away with it because
    /// BepInEx 5 let ConfigurationManager supply the converter for every plugin at once; the 5.0
    /// ConfigurationManager registers none, so each mod has to register its own. BossNotifier and
    /// SPT-DynamicMaps do the same thing.
    /// </para>
    /// <para>
    /// Channels are written comma-separated in the invariant culture, matching the colour values
    /// already present in 5.0 config files. Reading is deliberately lenient about separators so that
    /// values written by another mod's converter still load. <c>AddConverter</c> is a no-op when a
    /// converter for the type already exists, so this can never displace another plugin's.
    /// </para>
    /// </remarks>
    private static void RegisterColorConverter()
    {
        TomlTypeConverter.AddConverter(typeof(Color), new TypeConverter
        {
            ConvertToString = static (value, _) =>
            {
                Color color = (Color)value;

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3}",
                    color.r,
                    color.g,
                    color.b,
                    color.a);
            },
            ConvertToObject = static (text, _) =>
            {
                string[] channels = text.Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);

                if (channels.Length < 3)
                    return Color.white;

                return new Color(
                    ParseChannel(channels, 0),
                    ParseChannel(channels, 1),
                    ParseChannel(channels, 2),
                    channels.Length > 3 ? ParseChannel(channels, 3) : 1f);
            },
        });
    }

    private static float ParseChannel(string[] channels, int index)
    {
        return float.TryParse(channels[index], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : 0f;
    }

    public static ConfigEntry<T> BindConfiguration<T>(this ConfigFile
        configFile,
        string section,
        string key,
        T defaultValue,
        string description,
        int order)
    {
        return configFile.Bind(
            section,
            key,
            defaultValue,
            new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = order })
        );
    }

    public static void CreateButton(this ConfigFile configFile,
        string configSection,
        string configEntryName,
        string buttonName,
        string description,
        Action @function,
        int? order = null)
    {
        Action<ConfigEntryBase> drawer = (ConfigEntryBase entry) =>
        {
            if (GUILayout.Button(buttonName, GUILayout.ExpandWidth(true)))
            {
                @function();
            }
        };

        ConfigDescription configDescription = new(
            description,
            null,
            new ConfigurationManagerAttributes { Order = order, CustomDrawer = drawer }
        );

        configFile.Bind(configSection, configEntryName, "", configDescription);
    }

    public static T GetValue<T>(this ConfigEntry<T> configEntry)
    {
        return configEntry.Value ?? (T)configEntry.DefaultValue;
    }

    public static string GetRGBHexCode(this ConfigEntry<Color> configEntry)
    {
        return ColorUtility.ToHtmlStringRGB(configEntry.GetValue());
    }

    public static bool IsEnabled(this ConfigEntry<bool> configEntry)
    {
        return configEntry.Value;
    }
}