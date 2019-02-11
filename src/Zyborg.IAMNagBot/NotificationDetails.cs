using Newtonsoft.Json;

namespace Zyborg.IAMNagBot
{
    public abstract class NotificationDetails
    {
        public abstract string Credential { get; }

        public abstract string Category { get; }

        public override string ToString() =>
            JsonConvert.SerializeObject(this);
    }

    public class PasswordWarning : NotificationDetails
    {
        public PasswordWarning(int daysLeft)
        {
            DaysLeft = daysLeft;
        }

        public override string Credential => "password";
        public override string Category => "warning";

        public int DaysLeft { get; }
    }

    public class PasswordExpired : NotificationDetails
    {
        public PasswordExpired(int daysExpired)
        {
            DaysExpired = daysExpired;
        }

        public override string Credential => "password";
        public override string Category => "expired";

        public int DaysExpired { get; }
    }

    public class AccessKeyWarning : NotificationDetails
    {
        public AccessKeyWarning(string id, int daysLeft)
        {
            AccessKeyId = id;
            DaysLeft = daysLeft;
        }

        public override string Credential => "accessKey";
        public override string Category => "warning";

        public string AccessKeyId { get; }
        public int DaysLeft { get; }
    }

    public class AccessKeyExpired : NotificationDetails
    {
        public AccessKeyExpired(string id, int daysExpired)
        {
            AccessKeyId = id;
            DaysExpired = daysExpired;
        }

        public override string Credential => "accessKey";
        public override string Category => "expired";

        public string AccessKeyId { get; }
        public int DaysExpired { get; }
    }


}