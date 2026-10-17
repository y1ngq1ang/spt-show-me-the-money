using System;
using System.Linq;
using BepInEx.Logging;

namespace SwiftXP.SPT.Common.Loggers;

public class SimpleSptLogger : ISimpleSptLogger
{
    private ManualLogSource? _logger;

    public SimpleSptLogger(string pluginGuid, string pluginVersion)
    {
        _logger = Logger.CreateLogSource($"{pluginGuid} (v{pluginVersion})");
    }

    public void LogDebug(object data)
    {
        Log(LogLevel.Debug, data);
    }

    public void LogError(object data)
    {
        Log(LogLevel.Error, data);
    }

    public void LogException(Exception exception)
    {
        string? firstLineOfStackTrace = exception
            .StackTrace
            ?.Split(System.Environment.NewLine)
            ?.FirstOrDefault();

        LogError($"An unexpected error occured. Message: {exception.Message}. f.l.o.s.t.: {firstLineOfStackTrace}");
    }

    public void LogInfo(object data)
    {
        Log(LogLevel.Info, data);
    }

    public void Log(LogLevel logLevel, object data)
    {
        _logger?.Log(logLevel, data);
    }
}
