using System.Collections.Generic;

namespace Zyborg.IAMNagBot
{
    public class Notifications : Dictionary<string, List<NotificationDetails>>
    {
        public void Add(string username, NotificationDetails notification)
        {
            // Get or create the notification list for the user
            if (!TryGetValue(username, out var notifyList))
            {
                notifyList = new List<NotificationDetails>();
                base.Add(username, notifyList);
            }
            notifyList.Add(notification);
        }
    }
}