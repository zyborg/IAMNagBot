# IAMNagBot

AWS Lambda to send out notifications about upcoming expirations for IAM resources

---

NagBot currently supports sending out notifications via SES emails or Slack.

## Deployment

There are a pair of Terraform (TF) modules [included](./infra/tf) that provide...

* [iam-nagbot](./infra/tf/iam-nagbot) - deployment and configuration of the IAMNagBot Lambda and supporting infrastructure
* [iam-selfservice](./infra/tf/iam-selfservice) - convenient way to grant IAM permissions to users to manage their own credentials

Each of the TF modules above have their own README's to describe their
use and configuration.  If you don't currently use Terraform to manage
your infrastructure, you should still be able to look over the TF module
code and glean the necessary AWS resources and steps necessary to deploy
the Lambda function, and translate that into other tooling such as
CloudFormation or simple CLI scripts.

## Configuration

You can configure the following settings:

| Setting | Description |
|-|-|
| **`PasswordExpiredInDays`** | Specify the maximum age of a Password. (int, defaults to 90)
| **`AccessKeyExpiredInDays`** | Specify the maximum age of an Access Key.
| **`EarlyWarningInDays`**     | Specifies the number of days before expiration to start "early warning" notifications.  (int, defaults to 5, may be 0 to disable)
| **Email** |
| **`EmailFrom`**              | By specifying a From address, you enable email support.
| **`DefaultEmailTo`**         | If email support is enabled and an IAM user does not have an email address tag, then|his email will be ema         |led instead if it is specified.
| **`AlwaysEmailTo`**          | If email support is enabled, this email address wil be used **always** regardless of an email tag on the IAM user.
| **Slack** |
| **`SlackOauthToken`**        | By specifying the the OAuth Token, you enable Slack support.
| **`DefaultSlackTo`**         | If Slack support is enabled and an IAM user does not have an slack tag, then this slack address will be used instead.  Can be a channel (starts with `#`) or user (starts with `@`).
| **`AlwaysSlackTo`**          | If Slack support is enabled, this Slack address wil be used **always** regardless of an email tag on the IAM user.  Can be a channel (starts with `#`) or user (starts with `@`).
| **Templates**
| **`TemplateUrl`**            | A URL that resolves to content to template content for a specific notification method, category and type.  Defaults to a set of built-in generic templates (packaged in the assembly).  See more details below.
| **`NotificationCountLimit`** | If set to a number greater than zero, then will stop after this number of notifications have been sent.  (int, default to 0)

## IAM User Tags

In order for NagBot to know where to send notifications, it looks for tags on the user
objects to identify email and Slack addresses:

| IAM User Tag | Description
|-|-|
| **`email`** | Email address to send notifications to.
| **`slack`** | Slack address to send notifications to.  Can be a user (starts with `@`) or a channel (starts with `+` -- tags don't allow `#` characters so it will be replaced).

## Notification Templates

The content of notification messages is resolved using the [Scriban](https://github.com/lunet-io/scriban/blob/master/doc/language.md)
template language, which is very similar to the popular `liquid`
template language but arguably more powerful and expressive.

In the context of evaluating a template, there is a model of variables that can be used
to access details of the specific notification being evaluated.  The model includes the
following elements:

| Variable | Description |
|-|-|
| **`notification_method`** | The method used to send the notification, at present this can be either **`email`** or **`slack`**.
| **`username`**            | The name of the IAM user that the notification is targetting.
| **`notification`**        | An object that provides additional details about the notification that are specific to the type of notification.  You access these additional details by using a *dotted notation* on this object, e.g. `notification.property` (see below).

### The `notification` Context Object

In addition to the common model details that are always available in the template
context, the model object `notification` provides further details that are specific
to the type of notification and it will contain some combination of the following elements.

These properties will always be available on the `notification` model object:

| Property | Description |
|----------|-------------|
| **Common**
| **`credential`**    | the type of credential being notified about, either **`password`** or **`accessKey`**. |
| **`category`**      | the category of the notification, either **`warning`** or **`expired`**. |
| **Type-specific**
| **`days_expired`**  | for category of `expired`, the number of days the credential has been expired
| **`days_left`**     | for category of `warning`, the number of days left before expiration
| **`access_key_id`** | for the credential type of `accessKey`, the ID of the specific Access Key that is being referenced (each user can have up to 2).

### Custom Template Content

By default, the template content is resolved to an internal set of built-in generic templates
(packaged as embedded resources with the assembly).  You can override the templates being
used by providing the **`TemplateUrl`** configuration setting (mentioned above). You can only
provide a single URL to override this setting, but the URL can resolve to a different,
notification-specific template for each combination of notification method, credential type and
category.  This is possible because the URL itself is first evaluated as a Scriban template.

So as an example, the Template URL can reference some HTTPS endpoint using these details to resolve to a different template for each notification, such as:

```
https://some-host-that-lambda-can-reach.com/some/path/to/templates/{{notification_method}}/{{notification.credential}}-{{notification.category}}.yml
```

#### Special Support for S3 URls

In addition to any URL that would be natively supported by the .NET platform for
streaming content from, special support has been added for S3 URLs that will be
accessed using the security context of the caller (i.e. the Lambda function).
The format of the URL is as follows:

```
s3://bucket-name/path/to/object/key.ext
```

### Template to YAML Deserialization

There is one additional aspect to notification templates that needs to be taken into
account -- the template content when evaluated using the Scriban template language needs
to resolve to a legal YAML object.  This resolved YAML object is then deserialized to an
actual method-specific data object.

This approach allows you to control many aspects of the notification in addition to the actual
notification message content, for example for the email method, you can control the from email
address, you can include CC and BCC addresses.

#### YAML Deserialization for Email

For the `email` notification method, the YAML content will be deserialized to an instance of the `Amazon.SimpleEmail.Model.SendEmailRequest` class which is documented [here](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SimpleEmail/TSendEmailRequest.html).

There are a number of properties that can be used to control the message being sent
including both text and HTML content.  Here is an example of a template resolved for
an **email** message sending out a **warning** about a soon-to-be-expired **password**
notification:

```yaml
Source: "{{email_from}}"
Destination:
  ToAddresses:
    - "{{email_to}}"
Message:
  Subject:
    Data: "[IAMNagBot] Password Expiring Soon"
  Body:
    Text:
      Data: |
        Your Password for user account {{username}} will be
        expiring in {{notification.days_left}} day(s).
        
        Please sign in to your account and rotate your password before it expires.
    Html:
      Data: |
        <h1>Message from IAMNagBot</h1>
        
        <p>
          Your password for user account <b><code>{{username}}</code></b>
          will be expiring in <b>{{notification.days_left}}</b> day(s).
        </p><p>
          Please sign in to your account and rotate your password before it expires.
        </p>
```

#### YAML Deserialization for Slack

For the `slack` notification method, the YAML content will be deserialized and
transformed to a JSON object that will be provided as a payload for invoking the
Slack **`chat.postMessage`** API method.  The template content should resolve to
a JSON payload with this API method's [arguments](https://api.slack.com/methods/chat.postMessage#arguments).

Here is an example of a template resolved for a **Slack** message with an **expired**
alert for an **accessKey**:

```yaml
channel: "{{slack_to}}"
text: |
  Your Access Key {{notification.access_key_id}} for user account {{username}}
  has been expired for {{notification.days_expired}} day(s).
  
  Please sign in to your account and rotate your Access Key today!
```
