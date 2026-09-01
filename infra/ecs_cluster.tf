resource "aws_ecs_cluster" "this" {
  name = "origination-dev"

  service_connect_defaults {
    namespace = aws_service_discovery_http_namespace.internal.arn
  }
}
