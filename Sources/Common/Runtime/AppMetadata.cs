using System.Reflection;

namespace SwiftXP.SPT.Common.Runtime;

public static class AppMetadata
{
    public static string Version =>
        Assembly
            .GetExecutingAssembly()
            ?.GetName()
            .Version
            ?.ToString(3) ?? "0.0.0";
}