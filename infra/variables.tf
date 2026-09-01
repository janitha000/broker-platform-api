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

variable "jwt_signing_key" {
  type        = string
  sensitive   = true
  description = "Must match Identity and Origination Jwt:Key. Override in tfvars for anything other than local dev."
  default     = "broker-platform-identity-dev-key-change-me!"
}

variable "my_ip" {
  type        = string
  description = "Your public IP with /32, for one-off EF migrate. Empty = no extra 1433 rule."
  default     = ""
}

variable "enable_service_connect_tls" {
  type        = bool
  description = "When true, creates ACM PCA + wires Service Connect TLS on Payment. Default false: keep the template, do not apply PCA (billed)."
  default     = false
}
