resource "aws_cloudwatch_log_group" "payment" {
  name              = "/ecs/payment-api"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "payment" {
  family                   = "payment-api"
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
    image     = "${aws_ecr_repository.payment.repository_url}:latest"
    essential = true
    portMappings = [{
      name          = "http"
      containerPort = 8080
      hostPort      = 8080
      protocol      = "tcp"
      appProtocol   = "http"
    }]
    healthCheck = {
      command     = ["CMD-SHELL", "timeout 2 bash -c ':> /dev/tcp/127.0.0.1/8080' || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 60
    }
    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.payment.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "ecs"
      }
    }
  }])
}

resource "aws_ecs_service" "payment" {
  name            = "payment-api"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.payment.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.public_subnets
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = true
  }

  service_connect_configuration {
    enabled   = true
    namespace = aws_service_discovery_http_namespace.internal.arn

    log_configuration {
      log_driver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.payment.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "service-connect"
      }
    }

    service {
      port_name      = "http"
      discovery_name = "payment-api"

      client_alias {
        dns_name = "payment-api"
        port     = 8080
      }
    }
  }
}
