
variable "lambda_package" {
    description = "Full path to the IAMNagBox Lambda package ZIP to be deployed."
}
variable "deploy_s3_bucket" {
    description = "The name of an S3 bucket to publish the Lambda package for deployment."
}
variable "deploy_s3_key" {
    description = "The key path in the S3 bucket to publish the Lambda package for deployment."
}

variable "lambda_env_vars" {
    description = "You should specify a map of environment variables to adjust the configuration."
    type        = "map"
    default     = {
        ## Need to have at least one value in the Lambda ENV configuration
        ## In practice this should never be an issue because we'll need at
        ## least one config value to enable either email or Slack messages
        NOOP = ""
    }
}

variable "_deploy_s3_storage_class" {
    default = "REDUCED_REDUNDANCY"
}
variable "_lambda_timeout" {
    default = "30"
}

variable "_cwevents_generate_cred_report_schedule" {
    description = "Specifies a cron or rate expression to invoke the generation of the Credential Report.  Cron are always expressed in GMT."
    default     = "cron(20 13 * * ? *)"
}
variable "_cwevents_process_cred_report_schedule" {
    description = "Specifies a cron or rate expression to invoke the processing of the Credential Report.  Cron are always expressed in GMT."
    default     = "cron(25 13 * * ? *)"
}


resource "aws_s3_bucket_object" "lambda_package" {
    bucket        = "${var.deploy_s3_bucket}"
    key           = "${var.deploy_s3_key}"
    storage_class = "${var._deploy_s3_storage_class}"

    ## The etag is *more-or-less* an MD5 hash of the file, which can be compared
    ## to the S3 computed etag to trigger an update if the two hashes don't match
    etag   = "${md5(file(var.lambda_package))}"
    ## Source path is relative to the "res" sub-dir of the TF config root
    source = "${var.lambda_package}"

    tags = "${map(
        "${var.tf_tag_name}", "${var.tf_tag_value}"
    )}"
}

## Deploy the Lambda Function
resource "aws_lambda_function" "iamnagbot" {
    depends_on = ["aws_s3_bucket_object.lambda_package"]

    function_name = "IAMNagBot"
    description   = "Inspects expiring IAM User credentials and sends out notifications."
    s3_bucket     = "${var.deploy_s3_bucket}"
    s3_key        = "${var.deploy_s3_key}"

    source_code_hash = "${base64sha256(file(var.lambda_package))}"
    # source_code_hash = "${data.aws_s3_bucket_object.lambda_package.metadata["Terraform_base64sha256"]}"

    role    = "${aws_iam_role.iamnagbot_lambda.arn}"
    handler = "Zyborg.IAMNagBot::Zyborg.IAMNagBot.Function::FunctionHandler"
    runtime = "dotnetcore2.0"

    ## Future enhancement?
    ## The Lambda can be deployed in a VPC if
    ## both subnet IDs and SG IDs are provided
    ## Have to deploy this as a VPC-internal Lambda
    ## so that it can reach the Vault endpoints
    # vpc_config {
    #     subnet_ids         = "${var.lambda_vpc_subnet_ids}"
    #     security_group_ids = "${var.lambda_vpc_security_group_ids}"
    # }
    
    timeout = "${var._lambda_timeout}"

    tags = "${map(
        "${var.tf_tag_name}", "${var.tf_tag_value}"
    )}"

    environment {
        variables = "${var.lambda_env_vars}"
    }
}


## We setup 2 CloudWatch Events Rules on a scheduled basis
##  * First, to generate a credential report
##  * Second, to process the credential report

resource "aws_cloudwatch_event_rule" "generate_cred_report" {
    name        = "IAMNagBot_GenerateCredReport"
    description = "Invokes IAMNagBot to generate an IAM Credential Report."
    is_enabled  = true
    schedule_expression = "${var._cwevents_generate_cred_report_schedule}"
}
resource "aws_lambda_permission" "cwevents" {
    statement_id  = "CWEvents-Invoke-IAMNagBot-Generate"
    principal     = "events.amazonaws.com"
    source_arn    = "${aws_cloudwatch_event_rule.generate_cred_report.arn}"
    action        = "lambda:InvokeFunction"
    function_name = "${aws_lambda_function.iamnagbot.function_name}"
}
resource "aws_cloudwatch_event_target" "generate_cred_report" {
    rule      = "${aws_cloudwatch_event_rule.generate_cred_report.name}"
    arn       = "${aws_lambda_function.iamnagbot.arn}"
    input     = "\"generate-report\""
    target_id = "Generate-Credential-Report"
}

resource "aws_cloudwatch_event_rule" "process_cred_report" {
    name        = "IAMNagBot_ProcessCredReport"
    description = "Invokes IAMNagBot to process notifications against an IAM Credential Report."
    is_enabled  = true
    schedule_expression = "${var._cwevents_process_cred_report_schedule}"
}
resource "aws_lambda_permission" "cwevents_process_cred_report" {
    statement_id  = "CWEvents-Invoke-IAMNagBot-Process"
    principal     = "events.amazonaws.com"
    source_arn    = "${aws_cloudwatch_event_rule.process_cred_report.arn}"
    action        = "lambda:InvokeFunction"
    function_name = "${aws_lambda_function.iamnagbot.function_name}"
}
resource "aws_cloudwatch_event_target" "process_cred_report" {
    rule      = "${aws_cloudwatch_event_rule.process_cred_report.name}"
    arn       = "${aws_lambda_function.iamnagbot.arn}"
    input     = "null"
    target_id = "Process-Credential-Report"
}
