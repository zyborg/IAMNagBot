using System.Collections.Generic;

namespace Zyborg.IAMNagBot
{
    public class FunctionSettings
    {
        /// Specify the maximum age of a Password.
        public int PasswordExpiredInDays { get; set; } = 90;
        /// Specify the maximum age of an Access Key.
        public int AccessKeyExpiredInDays { get; set; } = 90;
        /// Specifies the number of days before expiration to start
        /// "early warning" notifications.  (May be 0 to disable.)
        public int EarlyWarningInDays { get;set; } = 5;

        /// By specifying a From address, you enable email support.
        public string EmailFrom { get; set; }

        /// If email support is enabled and an IAM user does not have an
        /// email address tag, then this email will be emailed instead
        /// if it is specified.
        public string DefaultEmailTo { get; set; }

        /// If email support is enabled, this email address wil be used
        /// <b>always</b> regardless of an email tag on the IAM user.
        public string AlwaysEmailTo { get; set; }

        /// By specifying the the OAuth Token, you enable Slack support.
        public string SlackOauthToken { get; set; }

        /// If Slack support is enabled and an IAM user does not have
        /// an slack tag, then this slack address will be used instead.
        /// Can be a channel (starts with '#`) or user (starts with `@`).
        public string DefaultSlackTo { get; set; }

        /// If Slack support is enabled, this Slack address wil be used
        /// <b>always</b> regardless of an email tag on the IAM user.
        /// Can be a channel (starts with '#`) or user (starts with `@`).
        public string AlwaysSlackTo { get; set; }

        /// A URL that resolves to content to template content for a
        /// specific notification method, category and type.
        public string TemplateUrl { get; set; }
    }
}