resource "aws_cloudwatch_log_group" "identity" {
  name              = "/ecs/identity-api"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "identity" {
  family                   = "identity-api"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.execution.arn
  task_role_arn            = aws_iam_role.task.arn

  runtime_platform {
    operating_system_family = "LINUX"
    cpu_architecture        = "X86_64"
  }

  container_definitions = jsonencode([{
    name      = "api"
    image     = "${aws_ecr_repository.identity.repository_url}:latest"
    essential = true
    portMappings = [{
      containerPort = 8080
      protocol      = "tcp"
    }]
    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      },
      {
        name  = "Jwt__Issuer"
        value = "identity"
      },
      {
        name  = "Jwt__Audience"
        value = "broker-platform"
      },
      {
        name  = "Payment__BaseUrl"
        value = "http://payment-api.${aws_service_discovery_private_dns_namespace.internal.name}:8080"
      }
    ]
    secrets = [
      {
        name      = "ConnectionStrings__Identity"
        valueFrom = aws_secretsmanager_secret.identity_sql.arn
      },
      {
        name      = "Jwt__Key"
        valueFrom = aws_secretsmanager_secret.jwt.arn
      }
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.identity.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "ecs"
      }
    }
  }])
}

resource "aws_ecs_service" "identity" {
  name            = "identity-api"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.identity.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  health_check_grace_period_seconds = 120

  network_configuration {
    subnets          = module.vpc.public_subnets
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.identity.arn
    container_name   = "api"
    container_port   = 8080
  }

  depends_on = [aws_lb_listener_rule.identity_auth]
}
