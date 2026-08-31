resource "aws_service_discovery_http_namespace" "internal" {
  name        = "origination-dev"
  description = "ECS Service Connect namespace for east-west traffic"
}
