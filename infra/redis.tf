resource "random_password" "signalr_redis" {
  count   = var.enable_signalr_redis ? 1 : 0
  length  = 32
  special = false
}

resource "aws_security_group" "signalr_redis" {
  count  = var.enable_signalr_redis ? 1 : 0
  name   = "signalr-redis-sg"
  vpc_id = module.vpc.vpc_id

  ingress {
    description     = "Notification API to SignalR Redis"
    from_port       = 6379
    to_port         = 6379
    protocol        = "tcp"
    security_groups = [aws_security_group.notification.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_elasticache_subnet_group" "signalr" {
  count      = var.enable_signalr_redis ? 1 : 0
  name       = "origination-signalr"
  subnet_ids = module.vpc.public_subnets
}

resource "aws_elasticache_replication_group" "signalr" {
  count = var.enable_signalr_redis ? 1 : 0

  replication_group_id       = "origination-signalr"
  description                = "SignalR backplane for notification-api"
  engine                     = "redis"
  engine_version             = "7.1"
  node_type                  = "cache.t3.micro"
  num_cache_clusters         = 1
  port                       = 6379
  subnet_group_name          = aws_elasticache_subnet_group.signalr[0].name
  security_group_ids         = [aws_security_group.signalr_redis[0].id]
  at_rest_encryption_enabled = true
  transit_encryption_enabled = true
  auth_token                 = random_password.signalr_redis[0].result
  auth_token_update_strategy = "SET"
  automatic_failover_enabled = false
  apply_immediately          = true
}

resource "aws_secretsmanager_secret" "notification_signalr" {
  count = var.enable_signalr_redis ? 1 : 0
  name  = "notification/dev/signalr-redis"
}

resource "aws_secretsmanager_secret_version" "notification_signalr" {
  count     = var.enable_signalr_redis ? 1 : 0
  secret_id = aws_secretsmanager_secret.notification_signalr[0].id
  secret_string = join("", [
    aws_elasticache_replication_group.signalr[0].primary_endpoint_address,
    ":6379,ssl=True,abortConnect=False,password=",
    random_password.signalr_redis[0].result
  ])
}