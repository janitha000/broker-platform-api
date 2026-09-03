resource "aws_cloudwatch_log_group" "identity" {
  name              = "/ecs/identity-api"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "identity" {
  family                   = "identity-api"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "512"
  memory                   = "1024"
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
      name          = "http"
      containerPort = 8080
      hostPort      = 8080
      protocol      = "tcp"
      appProtocol   = "http"
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
        value = "http://payment-api:8080"
      },
      {
        name  = "Auth0__Domain"
        value = "dev-ggsd0s-z.us.auth0.com"
      },
      {
        name  = "Auth0__Audience"
        value = "https://api.broker-platform.com"
      },
      {
        name  = "Auth0__ClientId"
        value = "q6NBsRyhYywqbUR2wnFUo4o4FyDbXIQZ"
      },
      {
        name  = "Auth0__ManagementClientId"
        value = "KqScOfMotsI5olraxTZdcP4Z3cEsvnsB"
      },
      {
        name  = "Auth0__AppBaseUrl"
        value = "https://d9oy49gmln888.cloudfront.net"
      },
    ]
    secrets = [
      {
        name      = "ConnectionStrings__Identity"
        valueFrom = aws_secretsmanager_secret.identity_sql.arn
      },
      {
        name      = "Jwt__Key"
        valueFrom = aws_secretsmanager_secret.jwt.arn
      },
      {
        name      = "Auth0__ClientSecret"
        valueFrom = aws_secretsmanager_secret.auth0_client.arn
      },
      {
        name      = "Auth0__ManagementClientSecret"
        valueFrom = aws_secretsmanager_secret.auth0_management.arn
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

  depends_on = [
    aws_secretsmanager_secret_version.auth0_client,
    aws_secretsmanager_secret_version.auth0_management,
  ]
}

resource "aws_ecs_service" "identity" {
  name            = "identity-api"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.identity.arn
  desired_count   = var.ecs_desired_count
  launch_type     = "FARGATE"

  health_check_grace_period_seconds = 120

  network_configuration {
    subnets          = module.vpc.public_subnets
    security_groups  = [aws_security_group.identity.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.identity.arn
    container_name   = "api"
    container_port   = 8080
  }

  service_connect_configuration {
    enabled   = true
    namespace = aws_service_discovery_http_namespace.internal.arn

    log_configuration {
      log_driver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.identity.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "service-connect"
      }
    }
  }

  depends_on = [aws_lb_listener_rule.identity_auth]
}
