# README - Terraform Module: IAM Self-service

This Terraform (TF) module provide a general configuration for creating
IAM policies and groups to allow IAM users to manage their own credentials.

Specifically, 2 custom IAM policies and 2 IAM groups will be created, that
allow users assigned to the groups to manage their own respective credentials,
either Access Keys and/or Passwords.

## Parameters

The following parameters are _required_:

| Parameter | Description
|-|-|
| **`tf_tag_name`**  | The name of a resource Tag to assign to resources (see below).
| **`tf_tag_value`** | The value of a resource Tag to assign to resources (see below).

In addition to the above required parameters, you can optionally override the
following parameters.

| Parameter | Default | Description
|-|-|-|
| **`_selfservice_accesskey_policy_name`** | `IAM-SelfService-AccessKeys` | Name of Policy allowing users to manage their own Access Keys.
| **`_selfservice_password_policy_name`**  | `IAM-SelfService-Password`   | Name of Policy allowing users to manage their own Console Password.
| **`_selfservice_accesskey_group_name`**  | `IAM-SelfService-AccessKeys` | Name of Group allowing users to manage their own Access Keys.
| **`_selfservice_password_group_name`**   | `IAM-SelfService-Password`   | Name of Group allowing users to manage their own Console Password.

### TF-identifying Tags

Any resources that are defined by this module will automatically get
a Tag added (if tags are supported by the resource) defined by the
Tag Name and Value input parameters `tf_tag_name` and `tf_tag_value`
respectively.  This allows for quick identification of any resources
created by this TF module.

## References

This module is based on the following reference documentation:

* https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_examples_iam_credentials_console.html
* https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_passwords_enable-user-change.html
* https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_passwords_user-change-own.html
