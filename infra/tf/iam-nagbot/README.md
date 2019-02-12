# README - Terraform Module: IAMNagBot Lambda Deployment

This Terraform (TF) module supports deploying and configuring the IAMNagBot
Lambda Function, and related supporting resources.

Specifically it will create an IAM execution Role for the Lambda, the
Lambda Function itself, CloudWatch Events rules for scheduled execution
of the Lambda Function on a recurring basis, and all the necessary IAM
resources (such as policies and roles) to allow everything to work
together.

## Parameters

The following parameters are _required_:

| Parameter | Description
|-|-|
| **`tf_tag_name`**      | The name of a resource Tag to assign to resources (see below).
| **`tf_tag_value`**     | The value of a resource Tag to assign to resources (see below).
| **`lambda_package`**   | Full path to the IAMNagBox Lambda package ZIP to be deployed.
| **`deploy_s3_bucket`** | The name of an S3 bucket to publish the Lambda package for deployment.
| **`deploy_s3_key`**    | The key path in the S3 bucket to publish the Lambda package for deployment.
| **`lambda_env_vars`**  | Specifies map of environment variables to adjust the configuration (see below).

In addition to the above required parameters, you can optionally override the
following parameters.

| Parameter | Default | Description
|-|-|-|
| **`_lambda_role_name`**        | `IAMNagBot_Lambda_Role`               | Name of IAM Role that will be assigned to the Lambda.
| **`_lambda_role_policies`**    | `AWSLambdaVPCAccessExecutionRole` ARN | List of IAM Policy ARNs that will be attached to Lambda Role (see below).
| **`_deploy_s3_storage_class`** | `REDUCED_REDUNDANCY`                  | Storage class used to store the Lambda package in S3.
| **`_lambda_timeout`**          | `30`                                  | Lambda execution limit.  The default of 30s should be sufficient for most scenarios but can be overridden if your environment has a very large set of IAM Users to process.
| **`_cwevents_generate_cred_report_schedule`** | `cron(20 13 * * ? *)`  | Specifies a cron or rate to invoke the generation of the Credential Report (see below).  Cron are always expressed in GMT.
| **`_cwevents_process_cred_report_schedule`**  | `cron(25 13 * * ? *)`  | Specifies a cron or rate expression to invoke the processing of the Credential Report (see below).  Cron are always expressed in GMT.

### TF-identifying Tags

Any resources that are defined by this module will automatically get
a Tag added (if tags are supported by the resource) defined by the
Tag Name and Value input parameters `tf_tag_name` and `tf_tag_value`
respectively.  This allows for quick identification of any resources
created by this TF module.

### Lambda Environment Variables Configuration

As part of Lambda deployment, you can define a number of environment variables
which a Lambda function can use for adjusting its configuration settings.
IAMNagBot is no different and offers a number of required and optional settings
which can be controlled through it configuration settings, and thus through the
use of the Lambda Environment Variables.

The list of configuration settings that can be specified can be found in the
general README documentation for IAMNagBot found [here](../../../README.md).
To specify a configuration setting, take the Setting name in the README
and prefix it with `NAGBOT_` then provide it and its value in the map object
for the `lambda_env_vars` TF parameter.  See the sample below for examples.

### Lambda Role Policies

Every Lambda Function has an associated IAM Role that defines the allowed
permissions that Lambda Function is granted for execution.  There are a
minimum set of permissions that all Lambdas need in order to run in AWS.

AWS provides a set of AWS-managed policies that can be used for this purpose
and the default Role created by this module uses one of those.  You can
override the permissions that are assigned to the Lambda function by specifying
a list of IAM Policy ARNs in the parameter `_lambda_role_policies`.  If you
do override this parameter, make sure you include permissions to allow the
Lambda to execute, by either referencing one of the AWS-managed policies
mentioned above, or defining your own policy with the necessary permissions.

In addition to the general Lambda execution permissions, you may wish to
attach other permissions (via your own custom policies) that may grant
access to other resources for the Lambda Function.  The most common use
case for this would be to grant access to an S3 bucket (and key path)
to the IAMNagBot Lambda to retrieve its message templates.

### CloudWatch Events Scheduling

By default, this TF configuration will create two (2) [CloudWatch Events](https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/WhatIsCloudWatchEvents.html)
rules that are schedule-driven.  The first will generate an IAM Credential Report
and the second will process the generated report.  This two-step setup is
necessary because of the asynchronous nature in the way that generating
and then fetching a Credential Report works.

By default the CWEvents schedule is setup to run _every day of the week_
at `13:20 (GMT)` (`08:20 (EST)`) to generate the report
and `13:25 (GMT)` (`08:25 (EST)`) to process the report and send out any
notifications.  You can override the schedule by providing your own **rate**
or **cron** expression per the CWEvents [Schedule Expression for Rules](https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/ScheduledEvents.html).

If you do override, the important thing to remember is that two rules should be
scheduled relatively close, but that the Generate rule should come before the
Process rule and give it ample time to complete the IAM report.  In practice this
may only take a couple minutes but may depend on how many IAM users you have.

Also remember, `cron` expressions are _always_ specified in GMT, so do the math
to translate from your local timezone.

## Sample

Here is an example TF configuration using this module.

```hcl

module "iam-nagbot" {
    source = "path/to/iamnagbot/module"

    tf_tag_name  = "terraform"
    tf_tag_value = "IAMNagBot"

    lambda_package   = "${path.root}/res/Zyborg.IAMNagBot.zip"
    deploy_s3_bucket = "my-s3-bucket"
    deploy_s3_key    = "key/path/to/Zyborg.IAMNagBot.zip"

    lambda_env_vars  = {
        NAGBOT_EmailFrom      = "noreply@example.com"
        NAGBOT_DefaultEmailTo = "opsteam@example.com"

        NAGBOT_SlackOauthToken = "xoxp-12345678901-12345678901-123456789012-12345678901234567890123456789012"
        NAGBOT_DefaultSlackTo  = "#opsteam-channel"

        ## Uncomment for testing
        #NAGBOT_AlwaysEmailTo  = "testing@ezstest.com"
        #NAGBOT_AlwaysSlackTo   = "#testing-channel"

        NAGBOT_TemplateUrl = "s3://my-s3-bucket/iamnagbot-templates/{{notification_method}}/{{notification.credential}}-{{notification.category}}.yml"
    }

    ## Override some defaults
    _lambda_role_name = "IAMNagBot_Lambda_Role"
    _lambda_role_policies = [
        ## Requires min set of permissions for Lambda execution (AWS-managed)
        ,"arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
        ## Custom policy to give access to S3 bucket for templates
        ,"${aws_iam_policy.my_s3bucketpolicy.arn}"
    ]
}

```
