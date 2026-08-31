resource "aws_service_discovery_private_dns_namespace" "internal" {
  name        = "origination-dev.internal"
  description = "VPC DNS for service-to-service calls"
  vpc         = module.vpc.vpc_id
}

resource "aws_service_discovery_service" "payment" {
  name = "payment-api"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.internal.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }
}
