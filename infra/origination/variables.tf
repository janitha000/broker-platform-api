variable "aws_region" {
  type    = string
  default = "ap-southeast-2"
}

variable "db_username" {
  type    = string
  default = "origination_admin"
}

variable "db_password" {
  type      = string
  sensitive = true
}

variable "my_ip" {
  type        = string
  description = "Your public IP with /32, for one-off EF migrate. Empty = no extra 1433 rule."
  default     = ""
}