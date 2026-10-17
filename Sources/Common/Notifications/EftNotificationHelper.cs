using System;
using EFT.Communications;

namespace SwiftXP.SPT.Common.Notifications;

public static class EftNotificationHelper
{
    public static void SendAlert(string message)
    {
        NotificationsService.SendAlert(message);
    }

    public static void SendLongAlert(string message)
    {
        NotificationsService.SendLongAlert(message);
    }

    public static void SendNotice(string message)
    {
        NotificationsService.SendNotice(message);
    }

    public static void SendLongNotice(string message)
    {
        NotificationsService.SendLongNotice(message);
    }
}

public class NotificationsService
{
    private static readonly Lazy<NotificationsService> s_instance = new(() => new NotificationsService());

    private NotificationsService() { }

    public static void SendAlert(string message)
    {
        Send(message, ENotificationDurationType.Default, ENotificationIconType.Alert);
    }

    public static void SendLongAlert(string message)
    {
        Send(message, ENotificationDurationType.Long, ENotificationIconType.Alert);
    }

    public static void SendNotice(string message)
    {
        Send(message, ENotificationDurationType.Default, ENotificationIconType.Default);
    }

    public static void SendLongNotice(string message)
    {
        Send(message, ENotificationDurationType.Long, ENotificationIconType.Default);
    }

    public static void Send(string message, ENotificationDurationType duration, ENotificationIconType icon)
    {
        CustomNotification notification = new(message, duration, icon, null);
        NotificationManager.DisplayNotification(notification);
    }

    public static NotificationsService Instance => s_instance.Value;
}
