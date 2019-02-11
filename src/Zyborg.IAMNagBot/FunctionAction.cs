using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using CsvHelper;
using Newtonsoft.Json;

namespace Zyborg.IAMNagBot
{
    /// <summary>
    /// One of these is constructed to handle each invocation of the
    /// main entry point FunctionHandler.
    /// </summary>
    public class FunctionAction
    {
        private FunctionSettings _settings;
        private IAmazonIdentityManagementService _iamClient;
        private IAmazonSimpleEmailService _sesClient;
        private ILambdaContext _context;


        public FunctionAction(FunctionSettings settings,
            IAmazonIdentityManagementService iamClient,
            IAmazonSimpleEmailService sesClient,
            ILambdaContext context)
        {
            _settings = settings;
            _iamClient = iamClient;
            _sesClient = sesClient;
            _context = context;
        }


        bool IsEmailEnabled => !string.IsNullOrEmpty(_settings.EmailFrom);

        bool IsSlackEnabled => !string.IsNullOrEmpty(_settings.SlackOauthToken);

        void LogLine(string msg) => _context.Logger.LogLine(msg);

        public async Task GenerateReport()
        {
                LogLine("Generating Credential Report...");
                var crRequ = new GenerateCredentialReportRequest();
                var crResp = await _iamClient.GenerateCredentialReportAsync(crRequ);

                LogLine("Response: " + JsonConvert.SerializeObject(crResp));
                return;
        }

        public async Task ProcessReport()
        {
            // We build up a mapping of notifications by username
            var notifications = new Notifications();

            LogLine("Retrieving and Processing Credential Report...");
            try
            {
                var crRequ = new GetCredentialReportRequest();
                var crResp = await _iamClient.GetCredentialReportAsync(crRequ);

                LogLine("Retrieved CREDENTIAL REPORT"
                    + $" of format [{crResp.ReportFormat}]"
                    + $" generated at [{crResp.GeneratedTime}]");

                using (var stream = crResp.Content)
                using (var reader = new StreamReader(stream))
                using (var csv = new CsvReader(reader))
                {
                    var userRecords = csv.GetRecords<CredentialReportEntry>();
                    CheckUsers(userRecords, notifications);
                }

                await NotifyUsers(notifications);
            }
            catch (CredentialReportNotPresentException ex)
            {
                LogLine("ERROR:  Credential Report Not Present:");
                LogLine(ex.ToString());
                return;
            }
            catch (CredentialReportNotReadyException ex)
            {
                LogLine("ERROR:  Credential Report Not Ready:");
                LogLine(ex.ToString());
                return;
            }
            catch (CredentialReportExpiredException ex)
            {
                LogLine("ERROR:  Credential Report Expired:");
                LogLine(ex.ToString());
                return;
            }
        }

        private void CheckUsers(
            IEnumerable<CredentialReportEntry> entries,
            Notifications notifications)
        {
            var now = DateTime.Now;

            var entryCount = 0;

            foreach (var entry in entries)
            {
                if (entry.PasswordEnabled??false)
                    CheckPassword(entry.User, entry.PasswordLastChanged);
                if (entry.AccessKey1Active??false)
                    CheckAccessKey(entry.User, "AccessKey_1", entry.AccessKey1LastRotated);
                if (entry.AccessKey2Active??false)
                    CheckAccessKey(entry.User, "AccessKey_2", entry.AccessKey2LastRotated);
                ++entryCount;
            }

            LogLine($"Found [{entryCount}] entries in Credential Report.");

            void CheckPassword(string username, DateTime? lastRotated)
            {
                var pwAgeInDays = (int)(now - (lastRotated??DateTime.MinValue)).TotalDays;
                if (pwAgeInDays + _settings.EarlyWarningInDays < _settings.PasswordExpiredInDays)
                    // Age is OK, nothing to do
                    return;

                if (pwAgeInDays >= _settings.AccessKeyExpiredInDays)
                {
                    notifications.Add(username, new PasswordExpired(
                        daysExpired: pwAgeInDays - _settings.AccessKeyExpiredInDays));
                }
                else
                {
                    // Add an early warning with days left till expiration
                    notifications.Add(username, new PasswordWarning(
                        daysLeft: _settings.AccessKeyExpiredInDays - pwAgeInDays));
                }
            }

            void CheckAccessKey(string username, string accessKey, DateTime? lastRotated)
            {
                var akAgeInDays = (int)(now - (lastRotated??DateTime.MinValue)).TotalDays;
                if (akAgeInDays + _settings.EarlyWarningInDays < _settings.AccessKeyExpiredInDays)
                    // Age is OK, nothing to do
                    return;

                // Get or create the notification list for
                // the user since we definitely need to notify
                if (!notifications.TryGetValue(username, out var sendList))
                {
                    sendList = new List<NotificationDetails>();
                    notifications[username] = sendList;
                }

                if (akAgeInDays >= _settings.AccessKeyExpiredInDays)
                {
                    // Add an expired error with Key identifier and how many days since expired
                    notifications.Add(username, new AccessKeyExpired(accessKey,
                        daysExpired: akAgeInDays - _settings.AccessKeyExpiredInDays));
                }
                else
                {
                    // Add an early warning with Key identifier and days left till expiration
                    notifications.Add(username, new AccessKeyWarning(accessKey,
                        daysLeft: _settings.AccessKeyExpiredInDays - akAgeInDays));
                }
            }
        }

        private async Task NotifyUsers(Notifications notifications)
        {
            // We'll re-use this to query for each user's tags
            var userTagsRequ = new ListUserTagsRequest();

            var notifyCount = 0;

            foreach (var userNotifications in notifications)
            {
                // Get tags for IAM user and extract the email and slack if they have them
                var username = userNotifications.Key;
                string userEmail = null;
                string userSlack = null;

                // Root account is included in the report but we can't query
                // for tags so it will have to fallback to "default" behavior
                // as if there were no notification-related tags for the account
                if (username != Function.RootAccountName)
                {
                    userTagsRequ.UserName = userNotifications.Key;
                    var userTagsResp = await _iamClient.ListUserTagsAsync(userTagsRequ);
                    userEmail = userTagsResp.Tags.FirstOrDefault(
                        x => x.Key == Function.EmailUserTag)?.Value;
                    userSlack = userTagsResp.Tags.FirstOrDefault(
                        x => x.Key == Function.SlackUserTag)?.Value;
                }
                
                else continue;

                // IAM User Tags don't allow '#' character
                userEmail = userEmail?.Replace('+', '#');
                userSlack = userSlack?.Replace('+', '#');

                LogLine($"Sending Notification to [{username}]:");
                LogLine($"  * at email = [{userEmail}]:");
                LogLine($"  * at slack = [{userSlack}]:");

                if (IsEmailEnabled)
                    await NotifyUserByEmail(username, userEmail, userNotifications.Value);
                if (IsSlackEnabled)
                    await NotifyUserBySlack(username, userSlack, userNotifications.Value);

                ++notifyCount;

                if (_settings.NotificationCountLimit > 0
                    && notifyCount >= _settings.NotificationCountLimit)
                {
                    LogLine("REACHED LIMIT OF NUMBER NOTIFICATIONS TO BE SENT -- STOPPING");
                    break;
                }
            }

            LogLine($"Sent [{notifyCount}] notification(s) of a total of [{notifications.Count}]");
        }

        private async Task NotifyUserByEmail(string username, string emailAddress,
            IEnumerable<NotificationDetails> notifications)
        {
            if (!string.IsNullOrEmpty(_settings.AlwaysEmailTo))
                emailAddress = _settings.AlwaysEmailTo;
            if (string.IsNullOrEmpty(emailAddress))
                emailAddress = _settings.DefaultEmailTo;

            if (string.IsNullOrEmpty(emailAddress))
            {
                LogLine($"WARNING:  Unable to resolve target Email Address for username [{username}]; SKIPPING!");
                return;
            }

            var templateModel = new Dictionary<string, object>
            {
                ["notification_method"] = "email",
                ["username"] = username,
                ["email_to"] = emailAddress,
                ["email_from"] = _settings.EmailFrom,
            };

            foreach (var n in notifications)
            {
                templateModel["notification"] = n;
                var emailRequ = await TemplateManager.Resolve<SendEmailRequest>(
                    templateModel, _settings.TemplateUrl);
                LogLine("EMAIL-REQU: " + JsonConvert.SerializeObject(emailRequ, Formatting.Indented));

                var emailResp = await _sesClient.SendEmailAsync(emailRequ);
                LogLine("EMAIL-RESP: " + JsonConvert.SerializeObject(emailResp, Formatting.Indented));

                // For testing/debugging:
                //throw new Exception("STOPPING AFTER FIRST EMAIL SEND");
            }
        }

        private async Task NotifyUserBySlack(string username, string slackAddress,
            IEnumerable<NotificationDetails> notifications)
        {
            if (!string.IsNullOrEmpty(_settings.AlwaysSlackTo))
                slackAddress = _settings.AlwaysSlackTo;
            if (string.IsNullOrEmpty(slackAddress))
                slackAddress = _settings.DefaultSlackTo;

            if (string.IsNullOrEmpty(slackAddress))
            {
                LogLine($"WARNING:  Unable to resolve target Slack Address for username [{username}]; SKIPPING!");
                return;
            }

            var slack = new SlackChatPoster(_settings.SlackOauthToken);
            var templateModel = new Dictionary<string, object>
            {
                ["notification_method"] = "slack",
                ["username"] = username,
                ["slack_to"] = slackAddress,
            };

            foreach (var n in notifications)
            {
                templateModel["notification"] = n;
                var slackPayload = await TemplateManager.Resolve<Dictionary<string, object>>(
                    templateModel, _settings.TemplateUrl);
                LogLine("SLACK-REQU: " + JsonConvert.SerializeObject(slackPayload, Formatting.Indented));

                var slackResp = await slack.SendMessage(slackPayload);
                LogLine("SLACK-RESP: " + JsonConvert.SerializeObject(slackResp, Formatting.Indented));

                // For testing/debugging:
                //throw new Exception("STOPPING AFTER FIRST SLACK SEND");
            }
        }
    }
}
