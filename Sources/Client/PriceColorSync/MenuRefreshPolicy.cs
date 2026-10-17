namespace SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;

// Menu navigation alone never schedules another refresh. A raid must have
// been observed, and then the actual main-menu Show must have completed.
internal sealed class MenuRefreshPolicy
{
    private bool _startup = true;
    private bool _raidSeen;
    private bool _menuShown;

    internal void ObserveRaid(bool inRaid)
    {
        if (!inRaid)
            return;

        _raidSeen = true;
        _menuShown = false;
    }

    internal void MenuShown()
    {
        _menuShown = true;
    }

    internal string? PendingReason(bool inRaid, bool sessionReady)
    {
        if (inRaid || !sessionReady || !_menuShown)
            return null;

        return _startup ? "client startup" : _raidSeen ? "return from raid" : null;
    }

    internal void Requested()
    {
        _startup = false;
        _raidSeen = false;
    }
}
