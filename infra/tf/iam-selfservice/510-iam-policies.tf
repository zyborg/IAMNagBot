
// Based on:
//  https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_examples_iam_credentials_console.html
//  https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_passwords_enable-user-change.html
//  https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_passwords_user-change-own.html

data "aws_iam_policy_document" "iam_selfservice_accesskeys" {
    policy_id   = "IAM-SelfService-AccessKeys"
    statement {
        effect    = "Allow"
        actions   = [
            ,"iam:GetUser"
            ,"iam:GetLoginProfile"
            ,"iam:ListAccessKeys"
            ,"iam:GetAccessKeyLastUsed"
            ,"iam:CreateAccessKey"
            ,"iam:UpdateAccessKey"
            ,"iam:DeleteAccessKey"
        ]
        resources = ["arn:aws:iam::*:user/$${aws:username}"]
    }
}
data "aws_iam_policy_document" "iam_selfservice_password" {
    policy_id = "IAM-SelfService-Password"
    statement {
        effect    = "Allow"
        actions   = [
            ,"iam:GetAccountPasswordPolicy"
        ]
        resources = ["*"]
    }    
    statement {
        effect    = "Allow"
        actions   = [
            ,"iam:GetUser"
            ,"iam:GetLoginProfile"
            ,"iam:ChangePassword"
        ]
        resources = ["arn:aws:iam::*:user/$${aws:username}"]
    }
}

resource "aws_iam_policy" "iam_selfservice_accesskeys" {
    name        = "${var._selfservice_accesskey_policy_name}"
    description = "(${var.tf_tag_name}=${var.tf_tag_value}) Allows a user to manage their own Access Keys."
    policy      = "${data.aws_iam_policy_document.iam_selfservice_accesskeys.json}"
}
resource "aws_iam_policy" "iam_selfservice_password" {
    name        = "${var._selfservice_password_policy_name}"
    description = "(${var.tf_tag_name}=${var.tf_tag_value}) Allows a user to manage their own Console Login Password."
    policy      = "${data.aws_iam_policy_document.iam_selfservice_password.json}"
}


resource "aws_iam_group" "iam_selfservice_accesskeys" {
    name = "${var._selfservice_accesskey_group_name}"
}
resource "aws_iam_group_policy_attachment" "iam_selfservice_accesskeys" {
    group      = "${aws_iam_group.iam_selfservice_accesskeys.name}"
    policy_arn = "${aws_iam_policy.iam_selfservice_accesskeys.arn}"
}

resource "aws_iam_group" "iam_selfservice_password" {
    name = "${var._selfservice_password_group_name}"
}
resource "aws_iam_group_policy_attachment" "iam_selfservice_password" {
    group      = "${aws_iam_group.iam_selfservice_password.name}"
    policy_arn = "${aws_iam_policy.iam_selfservice_password.arn}"
}
