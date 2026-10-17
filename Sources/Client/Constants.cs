using System.Text.Json;

namespace SwiftXP.SPT.ShowMeTheMoney.Client;

public static class Constants
{
    public const string LoggerPrefix = "[Show Me The Money] ";

    public const string RemotePathToGetFleaPrices = "/showMeTheMoney/getFleaPrices";

    public const string RemotePathToGetPartialRagfairConfig = "/showMeTheMoney/getPartialRagfairConfig";

    /// <summary>
    /// Shared options for the JSON the mod pulls off its own server routes.
    /// </summary>
    /// <remarks>
    /// The server writes these payloads with ASP.NET Core's defaults, which serialise property names
    /// in camelCase, so matching has to be case-insensitive.
    /// </remarks>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
