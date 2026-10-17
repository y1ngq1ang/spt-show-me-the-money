using System;
using BepInEx.Logging;

namespace SwiftXP.SPT.Common.Loggers;

public interface ISimpleSptLogger
{
    void LogDebug(object data);

    void LogError(object data);

    void LogException(Exception exception);

    void LogInfo(object data);

    void Log(LogLevel logLevel, object data);
}