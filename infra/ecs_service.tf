resource "aws_cloudwatch_log_group" "api" {
  name              = "/ecs/origination-api"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "api" {
  family                   = "origination-api"
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
    image     = "${aws_ecr_repository.api.repository_url}:latest"
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
        name  = "Messaging__Provider"
        value = "EventBridge"
      },
      {
        name  = "Messaging__EventBusName"
        value = aws_cloudwatch_event_bus.broker.name
      },
      {
        name  = "Messaging__Source"
        value = "origination.broker-platform"
      },
      {
        name  = "Messaging__AwsRegion"
        value = var.aws_region
      }
    ]
    secrets = [
      {
        name      = "ConnectionStrings__Origination"
        valueFrom = aws_secretsmanager_secret.sql.arn
      },
      {
        name      = "Jwt__Key"
        valueFrom = aws_secretsmanager_secret.jwt.arn
      }
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.api.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "ecs"
      }
    }
  }])
}

resource "aws_ecs_service" "api" {
  name            = "origination-api"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = var.ecs_desired_count
  launch_type     = "FARGATE"

  health_check_grace_period_seconds = 120

  network_configuration {
    subnets          = module.vpc.public_subnets
    security_groups  = [aws_security_group.origination.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api.arn
    container_name   = "api"
    container_port   = 8080
  }

  # Client only: no `service` block, so Origination is discoverable by nobody
  # and can resolve namespace names. Reaching Payment is denied by payment-api-sg.
  service_connect_configuration {
    enabled   = true
    namespace = aws_service_discovery_http_namespace.internal.arn

    log_configuration {
      log_driver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.api.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "service-connect"
      }
    }
  }

  depends_on = [aws_lb_listener.http]
}