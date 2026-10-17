using EFT;
using SwiftXP.SPT.Common.EFT;

namespace SwiftXP.SPT.Common.Sessions;

public static class SptSession
{
    // SPT 5.0 removed the EftClientBackendSession class and exposes the session only as IEftSession.
    public static IEftSession Session => EFTGameExtensions.Session;
}
