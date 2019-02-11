
## These two vars should be used in concert to add a tag
## (or equivalent) to any resource created by this module:
## e.g.
##    tags = {
##        "${var.tf_tag_name}" = var.tf_tag_value
##    }

variable "tf_tag_name" {
    description = "Name of a tag used to mark all resources created by this module."
}

variable "tf_tag_value" {
    description = "The value of a tag used to mark all resources created by this module with the name `var.tf.tag_name`."
}
