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

variable "auth0_client_secret" {
  type        = string
  sensitive   = true
  description = "Auth0 Regular Web App (Broker Identity BFF) client secret. Set in terraform.tfvars."
}

variable "auth0_management_client_secret" {
  type        = string
  sensitive   = true
  description = "Auth0 M2M (Broker Identity Management) client secret. Set in terraform.tfvars."
}

variable "auth0_payment_client_secret" {
  type        = string
  sensitive   = true
  description = "Auth0 M2M (Identity Service → Payment API) client secret. Set in terraform.tfvars."
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

variable "ecs_desired_count" {
  type        = number
  description = "Fargate tasks per service. 0 = parked (no Fargate hours). Start from the ECS console by setting desired count to 1 when you need the APIs."
  default     = 0
}
